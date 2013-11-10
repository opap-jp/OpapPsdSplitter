using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PhotoshopFile;

namespace OpapPsdSplitter
{
    public class LayerNode
    {
        /// <summary>
        /// レイヤノード名を取得または設定します
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// レイヤを取得または設定します
        /// </summary>
        public Layer Layer { get; set; }

        /// <summary>
        /// 親ノードを取得または設定します
        /// </summary>
        public LayerNode Parent { get; set; }

        /// <summary>
        /// 子ノードを取得します
        /// </summary>
        public List<LayerNode> Children { get; private set; }

        /// <summary>
        /// 子ノードが存在するかどうかを取得します
        /// </summary>
        public bool HasChildren 
        {
            get 
            { 
                return Children.Count > 0;
            } 
        }

        /// <summary>
        /// ルートノードかどうかを取得します
        /// </summary>
        public bool IsRoot
        {
            get
            {
                return Parent == null;
            }
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public LayerNode()
        {
            Children = new List<LayerNode>();
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="layer">レイヤー</param>
        public LayerNode(Layer layer)
            :this()
        {
            this.Layer = layer;
            this.Name = layer.Name;
        }

        /// <summary>
        /// 子ノードを追加
        /// </summary>
        /// <param name="node">追加するノード</param>
        public void AddChildNode(LayerNode node)
        {
            node.Parent = this;
            Children.Add(node);
        }

        /// <summary>
        /// ルートノードから現在のノードに至る最短の全ノードを列挙します
        /// </summary>
        /// <returns></returns>
        public List<LayerNode> GetNodePath()
        {
            List<LayerNode> paths = new List<LayerNode>();

            if (!IsRoot)
            {

                paths.AddRange(Parent.GetNodePath());
                paths.Add(this);
            
            }
            
            return paths;
        }
        /// <summary>
        /// ルートノードから現在のノードに至る最短の全ノード名を列挙します
        /// </summary>
        /// <returns></returns>
        public List<string> GetPath()
        {
            return GetNodePath()
                    .Select(x => x.Name)
                    .ToList();
        }

        public string GetPathString()
        {
            return string.Join("-", GetPath());
        }


        public int GetDepth()
        {
            if (IsRoot)
            {
                return 0;
            }

            return Parent.GetDepth() + 1;
        }
        
        public List<LayerNode> Flatten()
        {
            //深さ優先探索

            List<LayerNode> nodes = new List<LayerNode>();

            nodes.Add(this);

            foreach (var c in Children)
            {
                nodes.AddRange(c.Flatten());
            }

            return nodes;
        }

        public List<Layer> GetAllLayers()
        {
            return Flatten()
                    .Where(x => x.Layer != null)
                    .Select(x => x.Layer)
                    .ToList();
        }

        public override string ToString()
        {
            return this.Name;
        }


    }
}
