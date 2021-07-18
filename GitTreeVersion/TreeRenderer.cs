using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Spectre.Console;

namespace GitTreeVersion
{
    public class TreeRenderer
    {
        public void WriteTree(TreeNode parentNode)
        {
            var queue = new Stack<(TreeNode, int, bool)>();
            queue.Push((parentNode, 0, false));

            while (queue.TryPop(out var tuple))
            {
                var (node, depth, last) = tuple;
                
                RenderNode(node, depth, last);

                for (var i = node.Children.Count - 1; i >= 0; i--)
                {
                    queue.Push((node.Children[i], depth + 1, i == node.Children.Count - 1)); 
                }
            }
        }

        private void RenderNode(TreeNode node, int depth, bool last)
        {
            for (var i = 0; i < depth; i++)
            {
                if (i == depth - 1)
                {
                    if (last)
                    {
                        Console.Write("└─ ");   
                    }
                    else
                    {
                        Console.Write("├─ ");
                    }
                }
                else
                {
                    Console.Write("│ ");
                }
            }
            
            AnsiConsole.MarkupLine(node.Name);
        }
    }

    public class TreeNode
    {
        public string Name { get; }
        public ImmutableList<TreeNode> Children { get; private set; }

        public TreeNode(string name, params TreeNode[] children)
        {
            Name = name;
            Children = children.ToImmutableList();
        }

        
        public TreeNode(string name, IEnumerable<TreeNode> children)
        {
            Name = name;
            Children = children.ToImmutableList();
        }

        public void AddChild(TreeNode treeNode)
        {
            Children = Children.Add(treeNode);
        }
    }
}