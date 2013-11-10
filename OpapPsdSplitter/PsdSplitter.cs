using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PhotoshopFile;
using System.IO;


namespace OpapPsdSplitter
{
    /// <summary>
    /// PSDファイルを分割する
    /// </summary>
    public class PsdSplitter
    {

        /// <summary>
        /// 入力ファイルパスを取得します
        /// </summary>
        public string InputFilePath { get; private set; }

        /// <summary>
        /// 入力PSDファイル内容を取得します
        /// </summary>
        public PsdFile InputPsdFile { get; private set; }


        /// <summary>
        /// 出力ディレクトリの先頭に付加するプリフィクスを取得または設定します
        /// </summary>
        public string OutputDirPrefix { get; set; }

        /// <summary>
        /// アニメーションレイヤグループ（「@」から始まるレイヤグループ）を展開するかどうかを取得または設定します
        /// </summary>
        public bool SplitAnimeLayer { get; set; }

        /// <summary>
        /// トップレベルグループを強制的に可視化するかどうかを取得または設定します
        /// </summary>
        public bool VisibleTopLevelGroup { get; set; }

        /// <summary>
        /// 読み込みに使用した非ユニコードのレイヤ名のエンコーディングを取得します。
        /// </summary>
        public Encoding NonUnicodeEncoding { get; private set; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="filepath">入力PSDファイルパス</param>
        /// <param name="nonUnicodeEncoding">非ユニコードのレイヤ名のエンコーディング</param>
        public PsdSplitter(string filepath, Encoding nonUnicodeEncoding)
        {
            InputFilePath = Path.GetFullPath(filepath);
            InputPsdFile =  new PsdFile();
            OutputDirPrefix = "out-";


            using (var fs = new FileStream(filepath, FileMode.Open, FileAccess.Read))
            {
                InputPsdFile.Load(fs, nonUnicodeEncoding);
            }
        }


        /// <summary>
        /// PSDファイルをトップレベルのレイヤグループ単位で分割します
        /// </summary>
        /// <param name="outdir_prefix">出力先フォルダのプリフィクス</param>
        /// <param name="split_anime_layer">アニメーションレイヤグループ（「@」から始まるレイヤグループ）の展開を許可します</param>
        public void Split()
        {

            //出力先ディレクトリを作成する
            string out_dir;

            out_dir = Path.Combine(Path.GetDirectoryName(InputFilePath), OutputDirPrefix + Path.GetFileName(InputFilePath));
            out_dir = Path.GetFullPath(out_dir);

            if (!Directory.Exists(out_dir))
            {
                Directory.CreateDirectory(out_dir);  
            }

            //PSDを分解する
            List<KeyValuePair<string, List<Layer>>> splitted_layers = SplitPsd();


            //分解したPSDそれぞれ保存する
            for (int i = 0; i < splitted_layers.Count; i++)
            {

                string name = splitted_layers[i].Key;
                List<Layer> layers = splitted_layers[i].Value;

                PsdFile psd_out = MakePsd(InputPsdFile, layers);

                string out_filename = string.Format("{0}-{1}.psd", Path.GetFileNameWithoutExtension(InputFilePath),  name);
                Array.ForEach(Path.GetInvalidFileNameChars(), x =>
                {
                    out_filename = out_filename.Replace(x, '_');
                });


                psd_out.Save(Path.Combine(out_dir, out_filename), Encoding.UTF8);

            }



        }



        /// <summary>
        /// PSDファイルをトップレベルのレイヤグループ単位で分割します
        /// </summary>
        /// <param name="psd_in">入力PSD</param>
        /// <param name="split_anime_layer">アニメーションレイヤグループ（「@」から始まるレイヤグループ）の展開を許可します</param>
        /// <returns>分割したレイヤオブジェクト</returns>
        protected  List<KeyValuePair<string, List<Layer>>> SplitPsd()
        {

            string animate_layer_prefix = "@";

            //結果格納用
            List<KeyValuePair<string, List<Layer>>> sprittedLayers = new List<KeyValuePair<string, List<Layer>>>();

            //レイヤの階層を解析してツリー構造に
            LayerNode rootNode = LayerTreeParser.ParseLayerTree(InputPsdFile);
            
            //トップレベルのレイヤまたはレイヤグループについて処理
            for (int i = 0; i < rootNode.Children.Count; i++)
            {
                LayerNode node = rootNode.Children[i];

                if (SplitAnimeLayer && node.Name.StartsWith(animate_layer_prefix))
                {
                    var normalNodes = node.Children
                                            .Where(x=>x.Layer == null || x.Layer.GetSectionType() == LayerSectionType.Layer);

                    foreach (var anode in normalNodes)
                    {
                        sprittedLayers.Add(MakeResultItem(i, anode));
                    }
                }
                else
                {
                    sprittedLayers.Add(MakeResultItem(i, node));
                }


            }
            
            return sprittedLayers;

        }

        protected KeyValuePair<string, List<Layer>> MakeResultItem(int index, LayerNode node)
        {

            string name = string.Format("{0:000}-{1}", index, node.GetPathString());
            List<Layer> out_layer_list = node.GetAllLayers();


            if (VisibleTopLevelGroup)
            {
                out_layer_list.Last().Visible = true;
            }

            return new KeyValuePair<string, List<Layer>>(
                    name,
                    out_layer_list);

        }

        public static int CountNormalLayer(IEnumerable<Layer> layers)
        {

            //leyersのうち、フォルダ以外の通常レイヤをカウントする
            int normal_layer_count = layers.Count((x) =>
            {
                LayerSectionInfo _sec = x.AdditionalInfo
                    .OfType<LayerSectionInfo>().FirstOrDefault();

                if (_sec == null)
                {
                    return true;
                }

                switch (_sec.SectionType)
                {
                    case LayerSectionType.OpenFolder:
                    case LayerSectionType.ClosedFolder:
                    case LayerSectionType.SectionDivider:

                        return false;
                }

                return true;
            });


            return normal_layer_count;
        }


        public static PsdFile MakePsd(PsdFile basefile, IEnumerable<Layer> layers)
        {

            PsdFile psd = new PsdFile();
            psd.ChannelCount = basefile.ChannelCount;
            psd.ColorMode = basefile.ColorMode;
            psd.BitDepth = basefile.BitDepth;
            psd.RowCount = basefile.RowCount;
            psd.ColumnCount = basefile.ColumnCount;
            psd.ImageCompression = basefile.ImageCompression;
            psd.Resolution = basefile.Resolution;

            // 画像データのスペースを確保する
            int imageSize = psd.RowCount * psd.ColumnCount;
            psd.Layers.Clear();
            for (short i = 0; i < psd.ChannelCount; i++)
            {
                var channel = new Channel(i, psd.BaseLayer);
                channel.ImageData = new byte[imageSize];
                channel.ImageCompression = psd.ImageCompression;
                psd.BaseLayer.Channels.Add(channel);
            }


            psd.Layers.AddRange(layers);

            return psd;

        }
    }
}
