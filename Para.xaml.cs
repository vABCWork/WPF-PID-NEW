using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Trend
{

    // PIDパラメータの各名称とその値を、一つのクラスとして定義
    // クラス名:　ParaData
    // メンバー: byte   id      (パラメータID)
    //           string name    (パラメータ名称)
    //           string st_dt　    (パラメータのデータ値)
    //
    // 
   
    //

    public class ParaData
    {
        public Byte id { get; set; }        // id

        public string name { get; set; }   // SV,P,I,D 等

        public string st_dt { get; set; } // データ (string型)

    }


    /// <summary>
    /// Para.xaml の相互作用ロジック
    /// </summary>
    public partial class Para : Window
    {
        public static ObservableCollection<ParaData> paradata_list;

        public static string[] para_name;       // PIDパラメータ名 (SV,P,I,D等)
        Byte selected_para_id;    // パラメータ書き込み時、選択されたパラメータのid


        public Para()
        {
            InitializeComponent();
            WindowStartupLocation = WindowStartupLocation.CenterOwner;   // 表示位置

          
            paradata_list = new ObservableCollection<ParaData>();   // クラス ParaDataのコレクションをデータバインディングするため、ObservableCollectionで生成

            para_name = new string[8];                 // PIDパラメータの個数分の配列
            para_name[0] = "SV";
            para_name[1] = "P";
            para_name[2] = "I";
            para_name[3] = "D";
            para_name[4] = "MR";
            para_name[5] = "Hys";
            para_name[6] = "H/C";       // heater/cooler 0:heater, 1:cooler
            para_name[7] = "Typ";       // PID計算式の種別 0:速度型 , 1:位置型


            for (byte i = 0; i < para_name.Length; i++)
            {
                ParaData paraData = new ParaData();
                paraData.id = i;                // パラメータ id
                paraData.name = para_name[i];   // パラメータ名
                paraData.st_dt = "0";           // データ(読み出し前の仮の値)
                paradata_list.Add(paraData);        // paradata_listへ初期値格納
            }


            this.Para_ListView.ItemsSource = paradata_list;          //　リストボックスへの表示


        }

        //
        // ListView で項目を選択した場合の処理
        // paradata_list.count = 0のままだと、例外が発生するため、reurn　している。
        private void Para_ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (paradata_list.Count == 0) return;      //   

            ParaData paraData = (ParaData)Para_ListView.SelectedItem; // 選択された項目

            selected_para_id = paraData.id;                 // 選択されたパラメータのid

            ParaName_TextBox.Text = paraData.name;           // 選択項目の名前

            ParaData paraData1 = paradata_list.FirstOrDefault(p => p.id == paraData.id); // 選択項目のidを持つ オブジェクトを得る

           　ParaValue_TextBox.Text = paraData1.st_dt; // 選択した項目の値
        }



        //
        //    SV,P,I,D等のパラメータ値を、paradata_listへ格納
        //
        //       id    name     受信バッファ     マイコンから送信されるデータ 　 
        //        0     SV       rcvBuf[16],[17]   (int16) 10倍した値 (例:300ならば30.0℃を表す)
        //        1      P       rcvBuf[18],[19]   (int16) 10倍した値 (例:10ならば1%を表す)
        //        2      I       rcvBuf[20],[21]   (int16)
        //        3      D       rcvBuf[22],[23]   (int16)
        //        4     MR       rcvBuf[24],[25]   (int16) 10倍した値 (例:100ならば10.0%を表す)
        //        5     Hys      rcvBuf[26],[27]   (int16) 10倍した値 (例:10ならば1.0℃を表す)
        //        6    heat_cool  rcvBuf[28]      
        //        7    pid_type   rcvBuf[29] 
        //

        public static void Set_parameter_data()
        {
            float t;
            Int16 dt;

            if (paradata_list == null) return;

             paradata_list.Clear();  // この後に、Para_ListView_SelectionChangedが呼ばれる


            for (byte i = 0; i < para_name.Length; i++)     //   0:SV, 1:P, 2:I, 3:D, 4:MR, 5:Hys, 6:heat_coo 7:pid_type
            {
                ParaData paraData = new ParaData();

                paraData.id = i;                // パラメータ id  para_index:parameter 

                paraData.name = para_name[i];   // パラメータ名

                if ( i == 0)         // パラメータ  0:SV の場合 
                {
                    dt = BitConverter.ToInt16(MainWindow.rcvBuf, 16);
                    t = (float)(dt / 10.0);
                    paraData.st_dt = t.ToString("F1");
                }
                else if ( i == 1)   // パラメータ  1:P の場合 
                {
                    dt = BitConverter.ToInt16(MainWindow.rcvBuf, 18);
                    t = (float)(dt / 10.0);
                    paraData.st_dt = t.ToString("F1");
                }
                else if (i == 2)   // パラメータ  2:I の場合 
                {
                    dt = BitConverter.ToInt16(MainWindow.rcvBuf, 20);
                    paraData.st_dt = dt.ToString("F0");
                }
                else if (i == 3)   // パラメータ  3:D の場合 
                {
                    dt = BitConverter.ToInt16(MainWindow.rcvBuf, 22);
                    paraData.st_dt = dt.ToString("F0");
                }

                else if (i == 4 )   // パラメータ  4:MR の場合 
                {
                    dt = BitConverter.ToInt16(MainWindow.rcvBuf, 24);
                    t = (float)(dt / 10.0);
                    paraData.st_dt = t.ToString("F1");
                }
                else if (i == 5)   // パラメータ  5:Hys の場合 
                {
                    dt = BitConverter.ToInt16(MainWindow.rcvBuf, 26);
                    t = (float)(dt / 10.0);
                    paraData.st_dt = t.ToString("F1");
                }

                else  if (i == 6)                // パラメータ 6:heat_cool  は、uint8型    
                {
                    paraData.st_dt = MainWindow.rcvBuf[28].ToString();  
                }
                else if ( i == 7)                  // パラメータ  7:pid_type は、uint8型    
                {
                    paraData.st_dt = MainWindow.rcvBuf[29].ToString();
                }   


                paradata_list.Add(paraData);        // paradata_listへ初期値格納

            }

        }




        // パラメータ値の書き込みボタン　
        // 手順: 
        //  1) 定周期モニタの停止
        //  1) コマンドと書き込みデータを送信バッファへ格納
        //  2)  WriteWaitTimerを開始
        //  3)  WriteWaitTimerの設定時間を経過
        //  4)  送信処理
        //  5)  ReStartTimerの開始　(定周期モニタの開始待ち)
        //  6) 　定周期モニタの再開
        // 

        private void ParaWrite_Button_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.SendIntervalTimer.Stop();          //　定周期モニタの停止

            try
            {
                UInt16 crc_cd;

                MainWindow.sendBuf[0] = 0x03;            　// 送信コマンド  0x03 パラメータ 書き込みコマンド
                MainWindow.sendBuf[1] = selected_para_id;  //　パラメータのid

                if (selected_para_id < 6)                  // パラメータ  0:SV, 1:P, 2:I, 3:D, 4:MR, 5:Hysの場合、float型の処理 
                {
                    float t = float.Parse(ParaValue_TextBox.Text); // 書き込みデータ (単精度浮動小数点型)

                    byte[] byteArray = BitConverter.GetBytes(t);  // 書き込みデータをバイト配列へ変換
                                                                  // t = 1.0の場合、byteArray[0]=0x00, byteArray[1]=0x00,byteArray[2]=0x80,byteArray[3]=0x3F
                                                                  // lowバイト側から、配列へ格納されている。
                    MainWindow.sendBuf[2] = byteArray[0];      //  単精度浮動小数点型データ 4byte
                    MainWindow.sendBuf[3] = byteArray[1];
                    MainWindow.sendBuf[4] = byteArray[2];
                    MainWindow.sendBuf[5] = byteArray[3];
                }
                else　　　　　　　　　　　　　　　　　 // パラメータ  6:heat_cool, 7:pid_type の場合、整数型の処理 
                {
                    Byte it = Byte.Parse(ParaValue_TextBox.Text);

                    MainWindow.sendBuf[2] = it;
                    MainWindow.sendBuf[3] = 0;
                    MainWindow.sendBuf[4] = 0;
                    MainWindow.sendBuf[5] = 0;
                }

                crc_cd = MainWindow.CRC_sendBuf_Cal(6);  // CRC計算

                MainWindow.sendBuf[6] = (Byte)(crc_cd >> 8); // CRCは上位バイト、下位バイトの順に送信
                MainWindow.sendBuf[7] = (Byte)(crc_cd & 0x00ff);

                MainWindow.sendByteLen = 8;                   // 送信バイト数

                MainWindow.WriteWaitTimer.Start();             // 送信待ちタイマの起動

            }
            catch (FormatException)
            {
                ParaValue_TextBox.Text = "Input Error";

            }

        }



        // Windowが閉じられる際の処理
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            MainWindow.para_window_cnt = 0;     // パラメータ用ウィンドウの表示個数のクリア

        }
    }
}
