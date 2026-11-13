using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using vatsys;

namespace ATISPlugin
{
    // The plugin's config files (Zulu, Codes, OFCW, presets) in the profile's Plugins\Configs folder.
    //
    // A file the profile ships is the profile's: it's used as it is and never replaced. A file the plugin downloaded
    // from the repository (because the profile didn't have it) is kept up to date instead: it's checked against the
    // repository each time the plugin starts, cheaply (the repository answers "not modified" when it hasn't
    // changed), and replaced when it has. Downloads are recorded in ATISDownloads.json beside them, with a
    // fingerprint of what was saved, so a downloaded file someone has since edited becomes theirs and is left alone.
    //
    // Copies downloaded before this was recorded are recognised by matching a version in the repository's history.
    // Anything else (a profile's own copy) is the profile's.
    //
    // Checking is silent, as with ozserver: offline, or with the repository down or answering with something
    // unusable, the copy already there is used.
    internal static class ConfigFiles
    {
        private const string RecordsFile = "ATISDownloads.json";

        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

        private static readonly HttpClient Client = new HttpClient();

        private static readonly SemaphoreSlim RecordsLock = new SemaphoreSlim(1, 1);

        public class Record
        {
            public string Url { get; set; }
            public string ETag { get; set; }
            public string Hash { get; set; }
            public DateTime FetchedUtc { get; set; }
        }

        // Whether each file in use is the profile's own (true) or downloaded (false), by repository file name.
        private static readonly Dictionary<string, bool> Owned = new Dictionary<string, bool>();

        public static bool IsProfileOwned(string fileName)
        {
            lock (Owned) return Owned.TryGetValue(fileName, out var owned) && owned;
        }

        // The file's content: the profile's own copy, or the repository's latest (falling back to the copy
        // downloaded before). validate is given the download and the copy here (null when there isn't one), and
        // returns why the download can't be used, or null when it can. Throws only when there's no copy at all and
        // none could be downloaded.
        public static async Task<string> Get(string fileName, string repositoryUrl, string configsPath, bool canSave, Func<string, string, string> validate)
        {
            var profileFileName = fileName.StartsWith("ATIS") ? fileName : "ATIS" + fileName;

            var path = configsPath == null ? null : Path.Combine(configsPath, profileFileName);

            var cached = path != null && File.Exists(path) ? File.ReadAllText(path) : null;

            var records = configsPath == null ? new Dictionary<string, Record>() : await ReadRecords(configsPath);

            records.TryGetValue(fileName, out var record);

            var owned = cached != null && !IsDownload(fileName, cached, record);

            lock (Owned) Owned[fileName] = owned;

            if (owned) return cached;

            var url = $"{repositoryUrl}/{fileName}";

            // Conditional only when the copy here is the one the tag was given for.
            var etag = cached != null && record != null && record.Hash == Hash(cached) ? record.ETag : null;

            var download = await Download(url, etag);

            if (download == null)
            {
                if (cached != null) return cached;

                throw new Exception($"Could not download {fileName}.");
            }

            if (download.NotModified)
            {
                await SaveRecord(configsPath, canSave, fileName, new Record { Url = url, ETag = record?.ETag, Hash = Hash(cached), FetchedUtc = DateTime.UtcNow });

                return cached;
            }

            string problem;

            try
            {
                problem = validate?.Invoke(download.Content, cached);
            }
            catch (Exception ex)
            {
                problem = ex.Message;
            }

            // Something wrong with it: keep what's here. With nothing here, it's better than nothing.
            if (problem != null && cached != null) return cached;

            if (path != null && canSave)
            {
                try
                {
                    Directory.CreateDirectory(configsPath);

                    File.WriteAllText(path, download.Content);

                    await SaveRecord(configsPath, canSave, fileName, new Record { Url = url, ETag = download.ETag, Hash = Hash(download.Content), FetchedUtc = DateTime.UtcNow });
                }
                catch (Exception ex)
                {
                    Errors.Add(new Exception($"Could not save {profileFileName} to the profile: {ex.Message}"), Plugin.DisplayName);
                }
            }

            return download.Content;
        }

        // Downloaded by the plugin and not edited since, as recorded; or, without a record, a version the repository
        // has had, downloaded before downloads were recorded.
        public static bool IsDownload(string fileName, string content, Record record)
        {
            var hash = Hash(content);

            if (record != null) return record.Hash == hash;

            return Known.TryGetValue(fileName, out var known) && known.Contains(hash);
        }

        // A fingerprint that ignores the differences saving can make: a BOM, line endings, and trailing whitespace.
        public static string Hash(string content)
        {
            var text = (content ?? string.Empty).TrimStart('﻿').Replace("\r\n", "\n").TrimEnd();

            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(text));

                return string.Concat(bytes.Take(8).Select(x => x.ToString("x2")));
            }
        }

        private class DownloadResult
        {
            public bool NotModified { get; set; }
            public string Content { get; set; }
            public string ETag { get; set; }
        }

        // Null when it can't be downloaded in time. Off the calling thread, as the host is resolved before the
        // timeout applies.
        private static async Task<DownloadResult> Download(string url, string etag)
        {
            try
            {
                var timeout = new CancellationTokenSource(Timeout);

                var request = Task.Run(async () =>
                {
                    try
                    {
                        using (var message = new HttpRequestMessage(HttpMethod.Get, url))
                        {
                            if (!string.IsNullOrWhiteSpace(etag) && EntityTagHeaderValue.TryParse(etag, out var tag)) message.Headers.IfNoneMatch.Add(tag);

                            using (var response = await Client.SendAsync(message, timeout.Token).ConfigureAwait(false))
                            {
                                if (response.StatusCode == HttpStatusCode.NotModified) return new DownloadResult { NotModified = true };

                                if (!response.IsSuccessStatusCode) return null;

                                var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                                if (string.IsNullOrWhiteSpace(content)) return null;

                                return new DownloadResult { Content = content, ETag = response.Headers.ETag?.ToString() };
                            }
                        }
                    }
                    catch
                    {
                        return null;
                    }
                });

                var done = await Task.WhenAny(request, Task.Delay(Timeout)).ConfigureAwait(false);

                return done == request ? await request.ConfigureAwait(false) : null;
            }
            catch
            {
                return null;
            }
        }

        private static async Task<Dictionary<string, Record>> ReadRecords(string configsPath)
        {
            await RecordsLock.WaitAsync().ConfigureAwait(false);

            try
            {
                return ReadRecordsFile(configsPath);
            }
            finally
            {
                RecordsLock.Release();
            }
        }

        private static Dictionary<string, Record> ReadRecordsFile(string configsPath)
        {
            try
            {
                var path = Path.Combine(configsPath, RecordsFile);

                if (!File.Exists(path)) return new Dictionary<string, Record>();

                return JsonConvert.DeserializeObject<Dictionary<string, Record>>(File.ReadAllText(path)) ?? new Dictionary<string, Record>();
            }
            catch
            {
                // Unreadable: files are recognised by the repository's history instead.
                return new Dictionary<string, Record>();
            }
        }

        private static async Task SaveRecord(string configsPath, bool canSave, string fileName, Record record)
        {
            if (configsPath == null || !canSave) return;

            await RecordsLock.WaitAsync().ConfigureAwait(false);

            try
            {
                var records = ReadRecordsFile(configsPath);

                records[fileName] = record;

                Directory.CreateDirectory(configsPath);

                File.WriteAllText(Path.Combine(configsPath, RecordsFile), JsonConvert.SerializeObject(records, Formatting.Indented));
            }
            catch
            {
                // Recognised by the repository's history next time instead.
            }
            finally
            {
                RecordsLock.Release();
            }
        }

        // Every version each file has had in the repository, fingerprinted as Hash does, to recognise copies
        // downloaded before downloads were recorded. Only needs versions up to this release: later downloads are
        // recorded.
        private static readonly Dictionary<string, HashSet<string>> Known = new Dictionary<string, HashSet<string>>
        {
            ["Zulu.json"] = new HashSet<string>
            {
                "9c1fdc3895f445e3", "d13836eb73353804", "a11672446813a2a4", "9eb51325cd4df413",
                "57a84a22d0950c17", "5821d05377232abb", "3cf0f31096e186b8", "74bc33dcc6a7ea92",
                "f32a9d5ca5d1781d", "c778d7c0fe12e3b7", "c8d2626674194cd1", "2c4ed3a9988e6458",
                "783e8115c4a6703b", "04ba15b3c63845e6", "6e47b2165c50095c", "993a5b66c46174d5",
                "71db7e4bc928f921", "99397112dac1a051", "f7619a16cf823fe2", "3dccdd680db55a3e",
                "f965247ed3f3405d", "7e8e74db2001a7a5", "447fe9b135405880", "059a22b72ce268e4",
                "431b85d177393a27", "556b2bf58426e532", "d1181c47d28928f6", "479730d291c5918a",
                "91733d99847c55cb", "9d7c5cde1cff7f39", "38c3653cc1dfdeab", "f1e92e6b72df5c0f",
                "1fef92739b218db9", "a7c7cd3cfa70fae3", "dac9e5aff3d040d9", "0ce6681c06ea05e2",
                "d890a7588609f03c", "b3143335960e62d5", "17b8d20f49368f0b", "5d0094de1cc5d073",
                "8d9013e8faf1c50f", "cac6dd938e60a772", "016e6e6a75d090e4", "1ba12acb3ef9da9f",
                "3da92b5377606f8c", "8bf2cc5b14d1faf8", "d5a86dac0940599a", "fe161b18afda8b5a",
                "2faffac5c4a6bcc0", "4133fd3bcd134c37", "a47119d3188e1ef0",
            },
            ["Codes.json"] = new HashSet<string>
            {
                "406b9d11aaf10e8a", "31b92b9f0d4bfa48",
            },
            ["OFCW.json"] = new HashSet<string>
            {
                "3df45c90356f6087", "737c469eb36bc5bf", "ccdb6d7d7a13d523", "a79bc672899aeb99",
                "1cc13479d5121e10", "1374827b0d748d3e", "66fb806318b168f1", "3201617b58edbb03",
                "206885d37bb189af", "2fb1dc1e44093145", "2af55a926fe6423e", "8b836651076b2704",
                "88617839183c5556", "a042ed14d7c5708d",
            },
            ["ATISPresets.xml"] = new HashSet<string>
            {
                "2f17c939ab256481", "72a0f198b6c1ed95", "94b2e4e51754ef8e", "aca48dc40c67c77f",
                "66948512ea13ceec", "a0f78d487f4c7268", "f55c2ced54c5b566", "8aa7cf9858e795d9",
                "b8685f3fef78c66f", "6d8bb2b17293fa18", "53d0a7276dc176fe", "f397a1ec3b8bf54b",
                "3bc4ea893c46ae6e", "1dd7f74a38357056", "0a900d3c3b923874", "58a58862290e7e8e",
            },
        };
    }
}
