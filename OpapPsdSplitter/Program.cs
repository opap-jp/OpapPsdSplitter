using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PhotoshopFile;
using System.IO;

namespace OpapPsdSplitter
{
    class Program
    {
        static int Main(string[] args)
        {
            int files_start_pos = 0; //ファイル指定部の開始インデックス
    
            // 引数からスイッチを取得する
            bool opt_recurse = false;
            bool opt_opap_anime = false;
            bool opt_show = false;
            string opt_outdir_prefix = "out-";
            string opt_encoding_name = "shift_jis";

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].StartsWith("-"))
                {
                    //TODO: もっと綺麗にする（MITライセンスで使えるgetopt的なのはないか？）
                    switch (args[i].Substring(1))
                    {
                        case "r":
                            opt_recurse = true;
                            break;
                        case "o":
                            opt_opap_anime = true;
                            break;
                        case "e":
                            opt_show = true;
                            break;
                        default:
                            if (args[i].StartsWith("--outdir-prefix="))
                            {
                                opt_outdir_prefix = args[i].Substring("--outdir-prefix=".Length);
                            }
                            else if (args[i].StartsWith("--non-unicode-encoding="))
                            {
                                opt_encoding_name = args[i].Substring("--non-unicode-encoding=".Length);
                            }
                            else
                            {
                                PrintUsage();
                                return 1;
                            }
                            break;
                    }

                }
                else
                {
                    files_start_pos = i;
                    break;
                }
            }



            // ファイル指定部を読み込む
            string[] files = args.Skip(files_start_pos).ToArray();


            //指定されたファイルのうちpsdファイルを抽出かつフルパスに変換

            List<string> psd_files = files
                .Where(f => File.Exists(f) && Path.GetExtension(f).ToLower() == ".psd")
                .Select(f => Path.GetFullPath(f))
                .ToList();



            //再帰的にpsdを検索して抽出
            if (opt_recurse)
            {
                var dirs = files.Where(f => Directory.Exists(f));

                foreach (var d in dirs)
                {
                    DirectoryInfo di = new DirectoryInfo(d);
                    var rf = di.GetFiles("*.psd", SearchOption.AllDirectories)
                            .Where(fi => !fi.Directory.Name.StartsWith(opt_outdir_prefix))
                            .Select(fi => fi.FullName);
                           
                    psd_files.AddRange(rf);
                    
                }
            }

            //同一ファイルの重複を排除

            List<string> psd_files_uniq = psd_files
                    .Select(f => Path.GetFullPath(f))
                    .Distinct()
                    .ToList();


            //処理を行う
            Encoding enc = Encoding.GetEncoding(opt_encoding_name);

            psd_files_uniq.ForEach(f =>
            {
                Console.WriteLine(f);
                PsdSplitter ps = new PsdSplitter(f, enc);
                ps.SplitAnimeLayer = opt_opap_anime;
                ps.OutputDirPrefix = opt_outdir_prefix;
                ps.VisibleTopLevelGroup = opt_show;
                ps.Split();
            });

            return 0;
        }


        static void PrintUsage()
        {
            Console.WriteLine("Usage: OpapPsdSplitter.exe [オプション]... [ファイル]...");
            Console.WriteLine("PSDファイルをトップレベルのレイヤーグループ単位で分割します。");
            Console.WriteLine("元のファイルには影響を与えません。");
            Console.WriteLine();
            Console.WriteLine("  -r     指定したディレクトリ及びサブディレクトリ内に存在する");
            Console.WriteLine("         psdファイルを再帰的に検索して、すべて処理します。");
            Console.WriteLine("  -o     OPAP-JP仕様のレイヤ構成において動画レイヤ（@マークで");
            Console.WriteLine("         始まる名称のレイヤ）に含まれるサブレイヤグループを");
            Console.WriteLine("         展開します。");
            Console.WriteLine("  -s     トップレベルのレイヤグループが非表示状態になっている場合");
            Console.WriteLine("         表示状態に変更する");
            Console.WriteLine("  --outdir-prefix=PREFIX");
            Console.WriteLine("         出力するフォルダ名の先頭に付けるプリフィクスを指定");
            Console.WriteLine("         します。規定値は out- です。引用符は使用できません。");
            Console.WriteLine("  --non-unicode-encoding=ENCODING");
            Console.WriteLine("         非ユニコードのレイヤ名のエンコーディングを指定します。");
            Console.WriteLine("         規定値は shift_jisです。引用符は使用できません。");



        }





    }
}
