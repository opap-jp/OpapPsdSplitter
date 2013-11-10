using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PhotoshopFile;

namespace OpapPsdSplitter
{
    public static class LayerTreeParser
    {


        public static LayerNode ParseLayerTree(PsdFile psd)
        {

            LayerNode root = new LayerNode();
                        
            ParseLayerTree(psd.Layers.GetEnumerator(), root);

            return root;

        }



        private static void ParseLayerTree(IEnumerator<Layer> layers, LayerNode parentNode)
        {
            while (layers.MoveNext())
            {
                Layer currentLayer = layers.Current;

                //ノード作成＆追加
                LayerNode newNode = new LayerNode(currentLayer);
                parentNode.AddChildNode(newNode);


                //レイヤ種類を取得
                LayerSectionInfo sec = currentLayer.AdditionalInfo
                    .OfType<LayerSectionInfo>().FirstOrDefault();

                LayerSectionType secType = LayerSectionType.Layer;
                if (sec != null)
                {
                    secType = sec.SectionType;
                }

                //レイヤ種類別処理
                switch (secType)
                {
                    case LayerSectionType.SectionDivider:
                        //フォルダ終了（逆順に並んでいるため、こちらが先に来る）


                        //ひとつ下の階層として以降のレイヤーを処理
                        newNode.Layer = null;
                        newNode.AddChildNode(new LayerNode(currentLayer));
                        ParseLayerTree(layers, newNode);


                        break;

                    case LayerSectionType.OpenFolder:
                    case LayerSectionType.ClosedFolder:
                        //フォルダ開始（逆順に並んでいるため、こちらが後に来る）

                        parentNode.Name = currentLayer.Name;

                        //処理終了（再帰脱出条件）
                        return;
                    case LayerSectionType.Layer:
                    default:
                        //通常レイヤ
                        
                        break;
                }

            }




        }
    }
}
