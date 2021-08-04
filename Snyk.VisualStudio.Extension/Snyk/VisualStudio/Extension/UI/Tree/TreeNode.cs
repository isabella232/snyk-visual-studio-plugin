﻿namespace Snyk.VisualStudio.Extension.UI.Tree
{
    using System.Collections.ObjectModel;

    /// <summary>
    /// Issue tree node.
    /// </summary>
    public class TreeNode
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TreeNode"/> class.
        /// </summary>
        public TreeNode() => this.Items = new ObservableCollection<TreeNode>();

        /// <summary>
        /// Gets or sets a value indicating whether title.
        /// If it's parent title it display project name with target CLI file.
        /// If it's leaf node it's display vulnerability package name and title.
        /// </summary>
        public virtual string Title { get; set; }

        /// <summary>
        /// Gets a value indicating whether icon for node.
        /// </summary>
        public virtual string Icon => SnykIconProvider.DefaultFileIconPath;

        /// <summary>
        /// Gets or sets a value indicating whether items.
        /// </summary>
        public ObservableCollection<TreeNode> Items { get; set; }
    }
}
