namespace GitTreeVersion
{
    public readonly struct GitRef
    {
        public string Name { get; }
        public bool IsDetached { get; }

        public GitRef(string name, bool isDetached)
        {
            Name = name;
            IsDetached = isDetached;
        }
    }
}