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
        /// コンストラクタ
        /// </summary>
        /// <param name="filepath">入力PSDファイルパス</param>
        public PsdSplitter(string filepath)
        {
            InputFilePath = Path.GetFullPath(filepath);
            InputPsdFile =  new PsdFile();
            OutputDirPrefix = "out-";

            using (var fs = new FileStream(filepath, FileMode.Open, FileAccess.Read))
            {
                InputPsdFile.Load(fs, Encoding.UTF8);
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

                string out_filename = string.Format("{0}-{1:000}-{2}.psd", Path.GetFileNameWithoutExtension(InputFilePath), i,  name);
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

            const int DEPTH_THRESHOLD_DEFAULT = 0;
            const int DEPTH_THRESHOLD_ANIMATE = 1;
            string animate_layer_prefix = "@";


            //結果格納用
            List<KeyValuePair<string, List<Layer>>> sprittedLayers = new List<KeyValuePair<string, List<Layer>>>();



            int depth = 0; //現在の階層の深さ
            int depth_threshold = DEPTH_THRESHOLD_DEFAULT; //psdを分割する深さの閾値（初期値、第0階層）


            Stack<string> layer_folder_path = new Stack<string>(); //レイヤのフォルダパス
            Stack<List<Layer>> layer_list_stack = new Stack<List<Layer>>();


            layer_list_stack.Push(new List<Layer>()); //第0階層のリストをpushしておく


            // レイヤグループは逆順に並んでいるので逆転させて処理する
            foreach (var layer in InputPsdFile.Layers.Reverse<Layer>())
            {
                List<Layer> current_layer_list = layer_list_stack.Peek();
                current_layer_list.Add(layer);

                LayerSectionInfo sec = layer.AdditionalInfo
                    .OfType<LayerSectionInfo>().FirstOrDefault();

                if (sec == null)
                {
                    continue;
                }

                switch (sec.SectionType)
                {
                    case LayerSectionType.OpenFolder:
                    case LayerSectionType.ClosedFolder:
                        //フォルダ開始

                        if (SplitAnimeLayer
                            && layer.Name.StartsWith(animate_layer_prefix)
                            && depth == DEPTH_THRESHOLD_DEFAULT)
                        {
                            //動画レイヤグループとしての処理モードに入る

                            depth_threshold = DEPTH_THRESHOLD_ANIMATE;

                            //第1階層基準のレイヤリストを新規作成して以後これを使用
                            current_layer_list = new List<Layer>();
                            layer_list_stack.Push(current_layer_list);
                        }

                        depth++;
                        layer_folder_path.Push(layer.Name);

                        break;

                    case LayerSectionType.SectionDivider:
                        //フォルダ終了

                        depth--;

                        string close_folder_name = layer_folder_path.Pop();

                        if (SplitAnimeLayer
                            && close_folder_name.StartsWith(animate_layer_prefix)
                            && depth == DEPTH_THRESHOLD_DEFAULT
                            && depth_threshold == DEPTH_THRESHOLD_ANIMATE)
                        {
                            //通常処理モードに戻す
                            depth_threshold = DEPTH_THRESHOLD_DEFAULT; //切り出し対象階層を第0階層に戻す

                            //第0階層基準のレイヤリストに戻す
                            layer_list_stack.Pop();
                            current_layer_list = layer_list_stack.Peek();
                        }

                        break;
                    default:
                        //その他

                        break;
                }



                if (depth == depth_threshold)
                {

                    if (CountNormalLayer(current_layer_list) > 0)
                    {
                        //通常レイヤが含まれている場合のみ出力する

                        if (VisibleTopLevelGroup)
                        {
                            current_layer_list.First().Visible = true;
                        }

                        string name = MakeLayerPathString(layer_folder_path, current_layer_list.First().Name);
                        List<Layer> out_layer_list = current_layer_list.Reverse<Layer>().ToList(); //逆順に戻す

                        sprittedLayers.Add(new KeyValuePair<string, List<Layer>>(
                                name,
                                out_layer_list));
                    }

                    current_layer_list.Clear();
                }

            }

            return sprittedLayers;

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

        public static string MakeLayerPathString(IEnumerable<string> folderPath, string name)
        {

            List<string> path = new List<string>();
            path.AddRange(folderPath);
            path.Add(name);


            return string.Join("-", path);

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
