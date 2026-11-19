#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Threading;

// IDA* returns the shortest route from the current position, including after manual detours.
// Search runs off the main thread and has no Unity object dependencies.
public static class PuzzleHintSolver
{
    public static int[] Solve(int[] board, int size, int[] knownSolution, CancellationToken token)
        => new Search(board, size, knownSolution, token).Run();

    private sealed class Search
    {
        private readonly int[] board;
        private readonly int size;
        private readonly int[] upperBound;
        private readonly CancellationToken token;
        private readonly List<int> path = new List<int>();
        private readonly Dictionary<(ulong, ulong), int> visited = new Dictionary<(ulong, ulong), int>();
        private int nodes;

        public Search(int[] state, int width, int[] solution, CancellationToken cancellation)
        {
            board = (int[])state.Clone(); size = width; upperBound = solution; token = cancellation;
        }

        public int[] Run()
        {
            token.ThrowIfCancellationRequested();
            int lower = Heuristic();
            // Every move flips the parity of the distance to the goal.
            if ((lower & 1) != (upperBound.Length & 1)) lower++;
            int blank = Array.IndexOf(board, -1);
            for (int bound = lower; bound < upperBound.Length; bound += 2)
            {
                visited.Clear();
                if (Visit(blank, -1, 0, bound)) return path.ToArray();
            }
            return (int[])upperBound.Clone();
        }

        private bool Visit(int blank, int previous, int depth, int bound)
        {
            if ((nodes++ & 1023) == 0) token.ThrowIfCancellationRequested();
            int estimate = Heuristic();
            if (depth + estimate > bound) return false;
            if (estimate == 0) return true;
            var key = Key();
            if (visited.TryGetValue(key, out int oldDepth) && oldDepth <= depth) return false;
            if (visited.Count > 250000) visited.Clear();
            visited[key] = depth;
            // Explore moves that reduce Manhattan distance first.
            for (int order = 0; order < 2; order++)
            for (int direction = 0; direction < 4; direction++)
            {
                int next = blank + (direction == 0 ? -size : direction == 1 ? size : direction == 2 ? -1 : 1);
                if (next < 0 || next >= board.Length || next == previous ||
                    Math.Abs(next / size - blank / size) + Math.Abs(next % size - blank % size) != 1) continue;
                int tile = board[next];
                bool improves = Distance(tile, blank) < Distance(tile, next);
                if (improves != (order == 0)) continue;
                board[blank] = tile; board[next] = -1; path.Add(next);
                if (Visit(next, blank, depth + 1, bound)) return true;
                path.RemoveAt(path.Count - 1); board[next] = tile; board[blank] = -1;
            }
            return false;
        }

        private int Distance(int tile, int cell) => Math.Abs(tile / size - cell / size) + Math.Abs(tile % size - cell % size);
        private int Heuristic()
        {
            int total = 0;
            for (int i = 0; i < board.Length; i++) if (board[i] >= 0) total += Distance(board[i], i);
            // Disjoint linear conflicts add an admissible two moves per conflicting pair.
            uint used = 0;
            for (int a = 0; a < board.Length; a++)
            {
                int first = board[a];
                if (first < 0 || (used & (1u << first)) != 0) continue;
                for (int b = a + 1; b < board.Length; b++)
                {
                    int second = board[b];
                    if (second < 0 || (used & (1u << second)) != 0) continue;
                    bool row = a / size == b / size && first / size == a / size && second / size == a / size && first > second;
                    bool column = a % size == b % size && first % size == a % size && second % size == a % size && first > second;
                    if (!row && !column) continue;
                    total += 2; used |= (1u << first) | (1u << second); break;
                }
            }
            return total;
        }

        private (ulong, ulong) Key()
        {
            ulong low = 0, high = 0;
            for (int i = 0; i < board.Length; i++)
            {
                ulong tile = (ulong)(board[i] + 1);
                if (i < 12) low |= tile << (i * 5);
                else high |= tile << ((i - 12) * 5);
            }
            return (low, high);
        }
    }
}
#endif
