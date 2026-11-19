using System;
using System.Collections.Generic;

public static class PuzzleShuffle
{
    private static Dictionary<string, byte> threeDistances;
    public sealed class Deal
    {
        public int[] Tiles { get; }
        public int[] Solution { get; }
        public int MinimumMoves => Solution.Length;

        internal Deal(int[] tiles, List<int> reverseMoves)
        {
            Tiles = (int[])tiles.Clone();
            Solution = reverseMoves.ToArray();
            Array.Reverse(Solution);
        }
    }

    public static Deal Create(int size, int seed)
    {
        if (size < 3 || size > 5) throw new ArgumentOutOfRangeException(nameof(size));
        var random = new Random(seed);
        if (size == 3) return CreateThree(random);
        int target = size == 4 ? 20 + 2 * random.Next(5) : 32 + 2 * random.Next(7);
        int last = size * size - 1;
        Deal best = null;
        int[] candidates = new int[4];
        for (int attempt = 0; attempt < 512; attempt++)
        {
            int[] tiles = new int[last + 1];
            for (int i = 0; i < last; i++) tiles[i] = i;
            tiles[last] = -1;
            int blank = last;
            var path = new List<int>(target);
            while (path.Count < target)
            {
                int count = 0;
                for (int direction = 0; direction < 4; direction++)
                {
                    int cell = blank + (direction == 0 ? -size : direction == 1 ? size : direction == 2 ? -1 : 1);
                    if (cell < 0 || cell > last || Math.Abs(cell / size - blank / size) + Math.Abs(cell % size - blank % size) != 1)
                        continue;
                    int tile = tiles[cell];
                    // Every scramble move increases the Manhattan lower bound by exactly one.
                    // Reversing the path solves it in exactly that many moves, proving optimality.
                    if (Distance(tile, blank, size) == Distance(tile, cell, size) + 1)
                        candidates[count++] = cell;
                }
                if (count == 0) break;
                int next = candidates[random.Next(count)];
                path.Add(blank);
                tiles[blank] = tiles[next];
                tiles[next] = -1;
                blank = next;
                // Preserve the initial empty square at bottom right.
                if (blank == last && (best == null || path.Count > best.MinimumMoves))
                {
                    best = new Deal(tiles, path);
                    if (path.Count == target) return best;
                }
            }
        }
        if (best != null) return best;
        // A four-move cycle also satisfies the optimality proof, for every supported size.
        int[] fallback = new int[last + 1];
        for (int i = 0; i < last; i++) fallback[i] = i;
        fallback[last] = -1;
        var fallbackPath = new List<int>();
        int empty = last;
        foreach (int next in new[] { last - size, last - size - 1, last - 1, last })
        {
            fallbackPath.Add(empty);
            fallback[empty] = fallback[next];
            fallback[next] = -1;
            empty = next;
        }
        return new Deal(fallback, fallbackPath);
    }

    private static int Distance(int tile, int cell, int size) =>
        Math.Abs(tile / size - cell / size) + Math.Abs(tile % size - cell % size);

    private static Deal CreateThree(Random random)
    {
        // The complete 8-puzzle graph is small enough to cache exact distances once.
        if (threeDistances == null)
        {
            threeDistances = new Dictionary<string, byte>(181440);
            var queue = new Queue<string>();
            const string goal = "012345678";
            threeDistances.Add(goal, 0);
            queue.Enqueue(goal);
            while (queue.Count > 0)
            {
                string state = queue.Dequeue();
                int blank = state.IndexOf('8');
                foreach (int cell in NeighborsThree(blank))
                {
                    char[] next = state.ToCharArray();
                    next[blank] = next[cell];
                    next[cell] = '8';
                    string key = new string(next);
                    if (threeDistances.ContainsKey(key)) continue;
                    threeDistances.Add(key, (byte)(threeDistances[state] + 1));
                    queue.Enqueue(key);
                }
            }
        }
        char[] order = "012345678".ToCharArray();
        string initial;
        do
        {
            for (int i = 7; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (order[i], order[j]) = (order[j], order[i]);
            }
            initial = new string(order);
        } while (!threeDistances.TryGetValue(initial, out byte depth) || depth < 10);
        var tiles = new int[9];
        for (int i = 0; i < 8; i++) tiles[i] = order[i] - '0';
        tiles[8] = -1;
        var solution = new List<int>();
        string current = initial;
        while (threeDistances[current] > 0)
        {
            int blank = current.IndexOf('8');
            foreach (int cell in NeighborsThree(blank))
            {
                char[] next = current.ToCharArray();
                next[blank] = next[cell];
                next[cell] = '8';
                string key = new string(next);
                if (threeDistances[key] != threeDistances[current] - 1) continue;
                solution.Add(cell);
                current = key;
                break;
            }
        }
        // Deal accepts reverse scramble moves; this branch already found the forward solution.
        solution.Reverse();
        return new Deal(tiles, solution);
    }

    private static IEnumerable<int> NeighborsThree(int blank)
    {
        if (blank >= 3) yield return blank - 3;
        if (blank < 6) yield return blank + 3;
        if (blank % 3 > 0) yield return blank - 1;
        if (blank % 3 < 2) yield return blank + 1;
    }
}
