

namespace RedisServer.Database.Model
{
    public class Skiplist
    {
        private const int MaxLevel = 16;
        private readonly double _probability = 0.5;
        private readonly Random _rand = new Random();

        private readonly SkipListNode _head = new SkipListNode(double.MinValue, string.Empty, MaxLevel);

        private int GenerateRandomLevel()
        {
            int level = 1;
            while (_rand.NextDouble() < _probability && level < MaxLevel)
            {
                level++;
            }
            return level;
        }

        public bool Search(int target)
        {
            var exist = false;

            var level = MaxLevel - 1;
            var current = _head;
            while (true)
            {
                if (level < 0) break;

                if (current.Forward[level] == null || current.Forward[level].Score > target)
                {
                    level -= 1;
                    continue;
                }

                if (current.Forward[level].Score == target)
                {
                    exist = true;
                    break;
                }

                if (current.Forward[level].Score < target)
                {
                    current = current.Forward[level];
                    continue;
                }

            }

            return exist;
        }

        public void Add(string member, double score)
        {
            int level = GenerateRandomLevel();
            var update = new SkipListNode[MaxLevel];
            var current = _head;

            for (int i = MaxLevel - 1; i >= 0; i--)
            {
                while (current.Forward[i] != null &&
                       (current.Forward[i].Score < score ||
                       (current.Forward[i].Score == score && string.CompareOrdinal(current.Forward[i].Member, member) < 0)))
                {
                    current = current.Forward[i];
                }
                update[i] = current;
            }

            var newNode = new SkipListNode(score, member, level);
            for (int i = 0; i < level; i++)
            {
                newNode.Forward[i] = update[i].Forward[i];
                update[i].Forward[i] = newNode;
            }
        }



        public bool Erase(double score, string key)
        {
            var update = new SkipListNode[MaxLevel];
            var current = _head;
            var currentLevel = MaxLevel;

            for (int i = currentLevel - 1; i >= 0; i--)
            {
                while (current.Forward[i] != null &&
                      (current.Forward[i].Score < score ||
                       (current.Forward[i].Score == score && string.Compare(current.Forward[i].Member, key) < 0)))
                {
                    current = current.Forward[i];
                }
                update[i] = current;
            }

            var target = current.Forward[0];
            if (target == null || target.Score != score || target.Member != key)
            {
                return false; // Not found
            }

            for (int i = 0; i < currentLevel; i++)
            {
                if (update[i].Forward[i] != target)
                    break;

                update[i].Forward[i] = target.Forward[i];
            }

            while (currentLevel > 1 && _head.Forward[currentLevel - 1] == null)
            {
                currentLevel--;
            }

            return true;
        }

        public List<string> RemoveRangeByScore(double min, double max)
        {
            var removed = new List<string>();
            var update = new SkipListNode[MaxLevel];
            var current = _head;

            for (var i = MaxLevel - 1; i >= 0; i--)
            {
                while (current.Forward[i] != null && current.Forward[i].Score < min)
                {
                    current = current.Forward[i];
                }

                update[i] = current;
            }

            current = current.Forward[0];

            while (current != null && current.Score <= max)
            {
                var next = current.Forward[0];
                string member = current.Member;

                for (var i = 0; i < MaxLevel; i++)
                {
                    if (update[i].Forward[i] == current)
                    {
                        update[i].Forward[i] = current.Forward[i];
                    }
                }

                removed.Add(member);

                current = next;
            }

            return removed;
        }

        public List<string> GetByRange(double min, double max)
        {
            var members = new List<string>();
            var update = new SkipListNode[MaxLevel];
            var current = _head;


            while (current.Forward[0] != null && current.Forward[0].Score < min)
            {
                current = current.Forward[0];
            }



            current = current.Forward[0];

            while (current != null && current.Score <= max)
            {
                string member = current.Member;

                members.Add(member);

                current = current.Forward[0];
            }

            return members;
        }

        public int? GetRank(string member)
        {
            int? valueSearched = null;

            var current = _head;
            var currentIndex = -1;

            while (current != null)
            {

                if (current.Member == member)
                {
                    valueSearched = currentIndex;
                    break;
                }
                current = current.Forward[0];
                currentIndex += 1;
            }

            return valueSearched;
        }

        public int? GetReversRank(string member)
        {

            var current = _head;
            var currentIndex = -1;

            var valueFound = false;

            while (current != null)
            {

                if (current.Member == member)
                {
                    valueFound = true;
                }
                current = current.Forward[0];

                if (valueFound)
                {
                    currentIndex += 1;
                }

            }

            return valueFound ? currentIndex : null;
        }


        public List<string> RemoveByRank(int start, int max)
        {
            var removed = new List<string>();
            var update = new SkipListNode[MaxLevel];
            var current = _head;
            int currentIndex = -1;

            while (currentIndex + 1 < start && current.Forward[0] != null)
            {
                current = current.Forward[0];
                currentIndex++;
            }

            for (int i = 0; i < MaxLevel; i++)
            {
                update[i] = current;
            }

            var node = current.Forward[0];
            int rank = start;
            while (node != null && rank <= max)
            {
                removed.Add(node.Member);
                node = node.Forward[0];
                rank++;
            }

            for (int i = 0; i < MaxLevel; i++)
            {
                var temp = update[i].Forward[i];
                while (temp != null && removed.Contains(temp.Member))
                {
                    temp = temp.Forward[i];
                }
                update[i].Forward[i] = temp;
            }

            return removed;
        }


        public class SkipListNode
        {
            public double Score;
            public string Member;
            public SkipListNode[] Forward;

            public SkipListNode(double score, string member, int level)
            {
                Score = score;
                Member = member;
                Forward = new SkipListNode[level];
            }
        }

    }
}