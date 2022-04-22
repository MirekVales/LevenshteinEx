namespace LevenshteinEx
{
    using System.Threading;
    using System.Collections.Generic;

    public sealed class LevEx
    {
        public static int CacheSize { get; set; } = 50;

        static readonly Dictionary<(int, string), CompiledAutomaton> compiledAutomatonsCache
            = new Dictionary<(int, string), CompiledAutomaton>(CacheSize);
        static readonly Queue<(int, string)> compiledAutomatonsCacheIndices
            = new Queue<(int, string)>(CacheSize);

        static readonly ReaderWriterLockSlim @lock = new ReaderWriterLockSlim();

        public static bool UseHopcroftKarp { get; set; } = true;

        public string Word { get; }
        
        readonly CompiledAutomaton compiledAutomaton;

        public LevEx(string word, int distance = 1)
        {
            Word = word;
            compiledAutomaton = Compile(word, distance);
        }

        static CompiledAutomaton Compile(string word, int distance)
        {
            if (CacheSize < 1)
                return CompileAutomaton(word, distance);

            @lock.EnterReadLock();

            try
            {
                if (compiledAutomatonsCache.TryGetValue((distance, word), out var existing))
                    return existing;
            }
            finally
            {
                @lock.ExitReadLock();
            }

            var auto = CompileAutomaton(word, distance);

            @lock.EnterWriteLock();
            try
            {
                return CacheValue(distance, word, auto);
            }
            finally
            {
                @lock.ExitWriteLock();
            }
        }

        static CompiledAutomaton CompileAutomaton(string word, int distance)
        {
            var auto = new LevenshteinAutomata(word, true).toAutomaton(distance);
            auto = Operations.determinize(auto);

            if (UseHopcroftKarp)
                auto = MinimizationOperations.minimize(auto);

            return new CompiledAutomaton(auto);
        }

        public bool Matches(string testedWord)
            => compiledAutomaton.Matches(testedWord);

        public static bool Matches(string word, string testedWord, int maxDistance)
            => Compile(word, maxDistance).Matches(testedWord);

        static CompiledAutomaton CacheValue(int distance, string word, CompiledAutomaton automaton)
        {
            while (compiledAutomatonsCacheIndices.Count >= CacheSize && CacheSize > 0)
            {
                var last = compiledAutomatonsCacheIndices.Dequeue();
                compiledAutomatonsCache.Remove(last);

#if DEBUG
                System.Diagnostics.Debug.WriteLine($"{nameof(LevEx)} Item removed from cache ({last})");
#endif
            }

            compiledAutomatonsCache[(distance, word)] = automaton;
            compiledAutomatonsCacheIndices.Enqueue((distance, word));

            return automaton;
        }
    }
}
