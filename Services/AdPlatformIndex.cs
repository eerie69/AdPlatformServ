using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace AdPlatformServ.Services
{
    public class AdPlatformIndex
    {
        private ImmutableDictionary<string, ImmutableHashSet<string>> _index =
            ImmutableDictionary.Create<string, ImmutableHashSet<string>>(StringComparer.OrdinalIgnoreCase);

        private readonly ILogger<AdPlatformIndex> _logger;
        public AdPlatformIndex(ILogger<AdPlatformIndex> logger) => _logger = logger;

        public void LoadFromString(string content)
        {
            var tmp = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

            using var reader = new StringReader(content ?? string.Empty);
            string? line;
            int lineNo = 0;

            while ((line = reader.ReadLine()) is not null)
            {
                lineNo++;
                line = line.Trim();
                if (string.IsNullOrWhiteSpace(line)) continue;

                var colonIdx = line.IndexOf(':');
                if (colonIdx <= 0)
                {
                    _logger.LogWarning("Строка {LineNo} пропущена: нет двоеточия. [{Line}]", lineNo, line);
                    continue;
                }

                var name = line[..colonIdx].Trim();
                var locPart = line[(colonIdx + 1)..].Trim();

                if (string.IsNullOrWhiteSpace(name))
                {
                    _logger.LogWarning("Строка {LineNo} пропущена: пустое имя площадки.", lineNo);
                    continue;
                }

                var rawLocs = locPart.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (rawLocs.Length == 0)
                {
                    _logger.LogWarning("Строка {LineNo} пропущена: нет локаций.", lineNo);
                    continue;
                }

                foreach (var raw in rawLocs)
                {
                    var loc = NormalizeLocation(raw);
                    if (loc is null)
                    {
                        _logger.LogWarning("Строка {LineNo}: некорректная локация '{Raw}' для '{Name}'.", lineNo, raw, name);
                        continue;
                    }

                    if (!tmp.TryGetValue(loc, out var set))
                    {
                        set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        tmp[loc] = set;
                    }
                    set.Add(name);
                }
            }

            var newIndex = tmp.ToImmutableDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.ToImmutableHashSet(StringComparer.OrdinalIgnoreCase),
                StringComparer.OrdinalIgnoreCase);

           
            Interlocked.Exchange(ref _index, newIndex);
            _logger.LogInformation("Индекс загружен: {Locations} локаций.", newIndex.Count);
        }

        public IReadOnlyList<string> Search(string location)
        {
            var loc = NormalizeLocation(location);
            if (loc is null) return Array.Empty<string>();

            var snapshot = _index;
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var prefix in EnumeratePrefixes(loc))
            {
                if (snapshot.TryGetValue(prefix, out var set))
                    result.UnionWith(set);
            }

            return result.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
        }

        public static string? NormalizeLocation(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            var s = raw.Trim();
            if (!s.StartsWith("/")) s = "/" + s;

            while (s.Contains("//"))
                s = s.Replace("//", "/");

            if (s.Length > 1 && s.EndsWith('/'))
                s = s.TrimEnd('/');

            return s.ToLowerInvariant();
        }

        private static IEnumerable<string> EnumeratePrefixes(string loc)
        {
            var current = loc;
            while (true)
            {
                yield return current;
                var idx = current.LastIndexOf('/');
                if (idx <= 0) break;
                current = current[..idx];
                if (current.Length <= 0) break;
            }
        }
    }
}