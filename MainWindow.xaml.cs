using Microsoft.Win32;
using ScottPlot;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Security.Policy;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Trend
{

    // 履歴(ヒストリ)データ　クラス
    // クラス名: HistoryData
    // メンバー:  double  data0
    //            double  data1
    //            double  data2
    //            double  data3
    //            double  data4
    //            double  data5
    //            double  data6
    //            double  data7
    //            double  data8
    //            double  data9
    //            double  data10
    //            double  data11
    //            double  data12
    //            double  dt
    //

    public class HistoryData
    {
        public double data0 { get; set; }       //  PV(ch1)
        public double data1 { get; set; }       //  SV
        public double data2 { get; set; }       //  ch2
        public double data3 { get; set; }       //  ch3
        public double data4 { get; set; }       //  ch4
        public double data5 { get; set; }       //  cjt
        public double data6 { get; set; }       //  MV_Out (0-100%に制限された値)
        public Byte  mode0 { get; set; }       //  mode_stop_run
        public Byte  mode1  { get; set; }       //  mode_auto_manual
        public double data7 { get; set; }       //  P_MV
        public double data8 { get; set; }      //  I_MV
        public double data9 { get; set; }      //  D_MV
        public double data10 { get; set; }      //  En
        public double dt { get; set; }         // 日時 (double型)
    }



    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {

        Byte mode_stop_run;       //   0:stop, 1:run  
        Byte mode_auto_manual;    //   0:auto, 1:manual	


        public static byte[] sendBuf;          // 送信バッファ   
        public static int sendByteLen;         //　送信データのバイト数

        public static byte[] rcvBuf;           // 受信バッファ
        public static int srcv_pt;             // 受信データ格納位置

        public static DateTime receiveDateTime;           // 受信完了日時

        public static DispatcherTimer SendIntervalTimer;  // タイマ　モニタ用　電文送信間隔   
        DispatcherTimer RcvWaitTimer;                   　// タイマ　受信待ち用 
        public static DispatcherTimer WriteWaitTimer;    // タイマ　書き込み送信待ち
        DispatcherTimer ReStartTimer;                    // タイマ データ書き込みコマンド送信後の、モニタ開始用

        public static ushort send_msg_cnt;              // 送信数 
        public static ushort disp_msg_cnt_max;          // 送受信文の表示最大数
                                             

        uint trend_data_item_max;             // 各リアルタイム　トレンドデータの保持数 

        double[] trend_data0;                 // トレンドデータ 0   PV(ch1)
        double[] trend_data1;                 // トレンドデータ 1   SV
        double[] trend_data2;                 // トレンドデータ 2   ch2
        double[] trend_data3;                 // トレンドデータ 3   ch3
        double[] trend_data4;                 // トレンドデータ 4   ch4
        double[] trend_data5;                 // トレンドデータ 5   cjt                                     // 
        double[] trend_data6;                 // トレンドデータ 6   MV
        double[] trend_data7;                 // トレンドデータ 7   P_MV                 
        double[] trend_data8;                 // トレンドデータ 8   I_MV     
        double[] trend_data9;                 // トレンドデータ 9   D_MV  
        double[] trend_data10;                // トレンドデータ 10  En

        double[] trend_dt;                    // トレンドデータ　収集日時

        ScottPlot.Plottable.ScatterPlot trend_scatter_0; // トレンドデータ0   PV(ch1)
        ScottPlot.Plottable.ScatterPlot trend_scatter_1; // トレンドデータ1   SV
        ScottPlot.Plottable.ScatterPlot trend_scatter_2; // トレンドデータ2   ch2
        ScottPlot.Plottable.ScatterPlot trend_scatter_3; // トレンドデータ3   ch3
        ScottPlot.Plottable.ScatterPlot trend_scatter_4; // トレンドデータ4   ch4
        ScottPlot.Plottable.ScatterPlot trend_scatter_5; // トレンドデータ5   cjt
        ScottPlot.Plottable.ScatterPlot trend_scatter_6; // トレンドデータ6   MV
        ScottPlot.Plottable.ScatterPlot trend_scatter_7; // トレンドデータ7   P_MV
        ScottPlot.Plottable.ScatterPlot trend_scatter_8; // トレンドデータ8   I_MV
        ScottPlot.Plottable.ScatterPlot trend_scatter_9; // トレンドデータ9   D_MV
        ScottPlot.Plottable.ScatterPlot trend_scatter_10; // トレンドデータ10  En


        public List<HistoryData> historyData_list;          // ヒストリデータ　データ収集時に使用
        public List<HistoryData> historyData_file_list;     // ヒストリデータ　ファイルからの読み出し時に使用

        ScottPlot.Plottable.ScatterPlot history_scatter_0;   // ヒストリデータ 0   PV
        ScottPlot.Plottable.ScatterPlot history_scatter_1;   // ヒストリデータ 1   SV
        ScottPlot.Plottable.ScatterPlot history_scatter_2;   // ヒストリデータ 2   ch2
        ScottPlot.Plottable.ScatterPlot history_scatter_3;   // ヒストリデータ 3   ch3
        ScottPlot.Plottable.ScatterPlot history_scatter_4;   // ヒストリデータ 4   ch4
        ScottPlot.Plottable.ScatterPlot history_scatter_5;   // ヒストリデータ 5   cjt
        ScottPlot.Plottable.ScatterPlot history_scatter_6;   // ヒストリデータ 6   MV
        ScottPlot.Plottable.ScatterPlot history_scatter_7;   // ヒストリデータ 7   P_MV
        ScottPlot.Plottable.ScatterPlot history_scatter_8;   // ヒストリデータ 8   I_MV
        ScottPlot.Plottable.ScatterPlot history_scatter_9;   // ヒストリデータ 9   D_MV
        ScottPlot.Plottable.ScatterPlot history_scatter_10;   // ヒストリデータ 10  En


        public static int para_window_cnt;                    //　パラメータ用ウィンドウの表示個数 ( Para.xaml.csで使用するため static)
        public static int commlog_window_cnt;                 // 通信ログ用ウィンドウの表示個数

        public MainWindow()
        {
            InitializeComponent();

            ConfSerial.serialPort = new SerialPort();    // シリアルポートのインスタンス生成

            ConfSerial.serialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);  // データ受信時のイベント処理

            sendBuf = new byte[2048];     // 送信バッファ領域  serialPortのWriteBufferSize =2048 byte(デフォルト)
            rcvBuf = new byte[4096];      // 受信バッファ領域   SerialPort.ReadBufferSize = 4096 byte (デフォルト)

            disp_msg_cnt_max = 1000;        // 送受信文の表示最大数

            SendIntervalTimer = new System.Windows.Threading.DispatcherTimer();　　// タイマーの生成(定周期モニタ用)
            SendIntervalTimer.Tick += new EventHandler(SendIntervalTimer_Tick);  // タイマーイベント
            SendIntervalTimer.Interval = new TimeSpan(0, 0, 0, 0, 1000);         // タイマーイベント発生間隔 1sec(コマンド送信周期)

            RcvWaitTimer = new System.Windows.Threading.DispatcherTimer();　 // タイマーの生成(受信待ちタイマ)
            RcvWaitTimer.Tick += new EventHandler(RcvWaitTimer_Tick);        // タイマーイベント
            RcvWaitTimer.Interval = new TimeSpan(0, 0, 0, 0, 1000);          // タイマーイベント発生間隔 (受信待ち時間)

            WriteWaitTimer = new System.Windows.Threading.DispatcherTimer();　 // タイマーの生成(書込み待ちタイマ)
            WriteWaitTimer.Tick += new EventHandler(WriteWaitTimer_Tick);        // タイマーイベント
            WriteWaitTimer.Interval = new TimeSpan(0, 0, 0, 0, 100);          // タイマーイベント発生間隔 (書き込み待ち時間)

            ReStartTimer = new System.Windows.Threading.DispatcherTimer();　　// タイマーの生成(書き込みコマンド送信後のモニタ開始用)
            ReStartTimer.Tick += new EventHandler(ReStartTimer_Tick);         // タイマーイベント
            ReStartTimer.Interval = new TimeSpan(0, 0, 0, 0, 1000);           // タイマーイベント発生間隔 (モニタ開始待ち)


            historyData_list = new List<HistoryData>();     // モニタ時のトレンドデータ 記録用　
            historyData_file_list = new List<HistoryData>(); // ファイルからの読み出し時に使用

            Chart_Ini();                        // チャート(リアルタイム用)の初期化

            para_window_cnt = 0;
            commlog_window_cnt = 0;

            new Para();                         // パラメータ用のWindowをオープンせずにSaveした場合、paradata_listが無いため例外が発生する。その対策用
        }


        //
        //    wpfPlot_Trend　上で軸が変更された時に発生するイベント  (マウスのスクロールズームで発生)
        //    https://scottplot.net/faq/shared-axes/
        //
        private void wpfPlot_Trend_AxesChanged(object sender, EventArgs e)
        {
            var newAxisLimits = wpfPlot_Trend.Plot.GetAxisLimits();  // 新しい 範囲を得る

            // 他のグラフに対して、新しい範囲を設定後、描画
            wpfPlot_Trend_MV.Configuration.AxesChangedEventEnabled = false;
            wpfPlot_Trend_MV.Plot.SetAxisLimits(xMin: newAxisLimits.XMin, xMax: newAxisLimits.XMax);   // X軸の範囲だけ変更　(Y軸の範囲は変更しない)
            wpfPlot_Trend_MV.Render();
            wpfPlot_Trend_MV.Configuration.AxesChangedEventEnabled = true;
        }

        //
        //    wpfPlot_Trend_MV　上で軸が変更された時に発生するイベント  (マウスのスクロールズームで発生)

        private void wpfPlot_Trend_MV_AxesChanged(object sender, EventArgs e)
        {
            var newAxisLimits = wpfPlot_Trend_MV.Plot.GetAxisLimits();  // 新しい 範囲を得る
                                                                        // 他のグラフに対して、新しい範囲を設定後、描画
            wpfPlot_Trend.Configuration.AxesChangedEventEnabled = false;
            wpfPlot_Trend.Plot.SetAxisLimits(xMin: newAxisLimits.XMin, xMax: newAxisLimits.XMax);   // X軸の範囲だけ変更　(Y軸の範囲は変更しない)
            wpfPlot_Trend.Render();
            wpfPlot_Trend.Configuration.AxesChangedEventEnabled = true;

        }


        //
        //    wpfPlot_History　上で軸が変更された時に発生するイベント  (マウスのスクロールズームで発生)

        private void wpfPlot_History_AxesChanged(object sender, EventArgs e)
        {
            var newAxisLimits = wpfPlot_History.Plot.GetAxisLimits();  // 新しい 範囲を得る

            // 他のグラフに対して、新しい範囲を設定後、描画
            wpfPlot_History_MV.Configuration.AxesChangedEventEnabled = false;
            wpfPlot_History_MV.Plot.SetAxisLimits(xMin: newAxisLimits.XMin, xMax: newAxisLimits.XMax);   // X軸の範囲だけ変更　(Y軸の範囲は変更しない)
            wpfPlot_History_MV.Render();
            wpfPlot_History_MV.Configuration.AxesChangedEventEnabled = true;
        }

        //
        //    wpfPlot_History_MV　上で軸が変更された時に発生するイベント  (マウスのスクロールズームで発生)
        private void wpfPlot_History_MV_AxesChanged(object sender, EventArgs e)
        {

            var newAxisLimits = wpfPlot_History_MV.Plot.GetAxisLimits();  // 新しい 範囲を得る
                                                                        // 他のグラフに対して、新しい範囲を設定後、描画
            wpfPlot_History.Configuration.AxesChangedEventEnabled = false;
            wpfPlot_History.Plot.SetAxisLimits(xMin: newAxisLimits.XMin, xMax: newAxisLimits.XMax);   // X軸の範囲だけ変更　(Y軸の範囲は変更しない)
            wpfPlot_History.Render();
            wpfPlot_History.Configuration.AxesChangedEventEnabled = true;
        }

        // 　チャートの初期化(リアルタイム　チャート用)
        //  https://swharden.com/scottplot/faq/version-4.1/
        //
        private void Chart_Ini()
        {
            trend_data_item_max = 30;             // 各リアルタイム　トレンドデータの保持数(=30 ) 1秒毎に収集すると、30秒分のデータ

            trend_data0 = new double[trend_data_item_max];
            trend_data1 = new double[trend_data_item_max];
            trend_data2 = new double[trend_data_item_max];
            trend_data3 = new double[trend_data_item_max];
            trend_data4 = new double[trend_data_item_max];
            trend_data5 = new double[trend_data_item_max];
            trend_data6 = new double[trend_data_item_max];
            trend_data7 = new double[trend_data_item_max];
            trend_data8 = new double[trend_data_item_max];
            trend_data9 = new double[trend_data_item_max];
            trend_data10 = new double[trend_data_item_max];
            trend_dt = new double[trend_data_item_max];

            DateTime datetime = DateTime.Now;   // 現在の日時

            DateTime[] myDates = new DateTime[trend_data_item_max];


            for (int i = 0; i < trend_data_item_max; i++)
            {
                trend_data0[i] = i;
                trend_data1[i] = i + 1;
                trend_data2[i] = i + 2;
                trend_data3[i] = i + 3;
                trend_data4[i] = i + 4;
                trend_data5[i] = i + 5;
                trend_data6[i] = i + 6;
                trend_data7[i] = i + 7;
                trend_data8[i] = i + 8;
                trend_data9[i] = i + 9;
                trend_data10[i] = i + 10;

                myDates[i] = datetime + new TimeSpan(0, 0, i);  // i秒増やす

                trend_dt[i] = myDates[i].ToOADate();   // (現在の日時 + i 秒)をdouble型に変換
            }

            // X軸の日時リミットを、最終日時+1秒にする
            DateTime dt_end = DateTime.FromOADate(trend_dt[trend_data_item_max - 1]); // double型を　DateTime型に変換
            TimeSpan dt_sec = new TimeSpan(0, 0, 1);    // 1 秒
            DateTime dt_limit = dt_end + dt_sec;      // DateTime型(最終日時+ 1秒) 
            double dt_ax_limt = dt_limit.ToOADate();   // double型(最終日時+ 1秒) 

            wpfPlot_Trend.Refresh();        // データ変更後のリフレッシュ
            wpfPlot_Trend_MV.Refresh();

            trend_scatter_0 = wpfPlot_Trend.Plot.AddScatter(trend_dt, trend_data0, color: System.Drawing.Color.Blue,  label: "PV(ch1)"); // プロット plot the data array only once
            trend_scatter_1 = wpfPlot_Trend.Plot.AddScatter(trend_dt, trend_data1, color: System.Drawing.Color.Gainsboro,label: "SV");
            trend_scatter_2 = wpfPlot_Trend.Plot.AddScatter(trend_dt, trend_data2, color: System.Drawing.Color.Orange, label: "ch2");

            trend_scatter_3 = wpfPlot_Trend.Plot.AddScatter(trend_dt, trend_data3, color: System.Drawing.Color.Green,  label: "ch3");     // ch3 (無効データ)
            trend_scatter_4 = wpfPlot_Trend.Plot.AddScatter(trend_dt, trend_data4, color: System.Drawing.Color.SkyBlue, label: "ch4");    // ch4 (無効データ)
            
            trend_scatter_5 = wpfPlot_Trend.Plot.AddScatter(trend_dt, trend_data5, color: System.Drawing.Color.Black, label: "cjt");

            trend_scatter_6 = wpfPlot_Trend_MV.Plot.AddScatter(trend_dt, trend_data6, color: System.Drawing.Color.Red, label: "MV");
            trend_scatter_7 = wpfPlot_Trend_MV.Plot.AddScatter(trend_dt, trend_data7, color: System.Drawing.Color.Pink, label: "P_MV");
            trend_scatter_8 = wpfPlot_Trend_MV.Plot.AddScatter(trend_dt, trend_data8, color: System.Drawing.Color.DarkRed, label: "I_MV");
            trend_scatter_9 = wpfPlot_Trend_MV.Plot.AddScatter(trend_dt, trend_data9, color: System.Drawing.Color.DarkOrange, label: "D_MV");
            trend_scatter_10 = wpfPlot_Trend_MV.Plot.AddScatter(trend_dt, trend_data10, color: System.Drawing.Color.DarkSlateGray, label: "En");

            // PVグラフ
            wpfPlot_Trend.Configuration.Pan = false;               // パン(グラフの移動)不可
            wpfPlot_Trend.Configuration.ScrollWheelZoom = true;   // ズーム(グラフの拡大、縮小)可

            wpfPlot_Trend.Plot.SetAxisLimits(trend_dt[0], dt_ax_limt, 0, 50);  // X軸の最小=0,X軸の最大=tred_data0の大きさ,Y軸最小=0, Y軸最大=50℃


            wpfPlot_Trend.Plot.XAxis.Ticks(true, false, true);         // X軸の大きい目盛り=表示, X軸の小さい目盛り=非表示, X軸の目盛りのラベル=表示
            wpfPlot_Trend.Plot.XAxis.TickLabelStyle( fontSize: 16);

            wpfPlot_Trend.Plot.XAxis.TickLabelFormat("HH:mm:ss", dateTimeFormat: true); // X軸　時間の書式(例 12:30:15)、X軸の値は、日時型

            wpfPlot_Trend.Plot.XAxis.Label(label: "time", color: System.Drawing.Color.Black);  // X軸全体のラベル
            wpfPlot_Trend.Plot.YAxis.TickLabelStyle(fontSize: 16);     // Y軸   ラベルのフォントサイズ変更  :

            wpfPlot_Trend.Plot.YAxis.Label(label: "[℃]", color: System.Drawing.Color.Black);    // Y軸全体のラベル


            // MV グラフ
            wpfPlot_Trend_MV.Configuration.Pan = false;               // パン(グラフの移動)不可
            wpfPlot_Trend_MV.Configuration.ScrollWheelZoom = true;   // ズーム(グラフの拡大、縮小)可

            wpfPlot_Trend_MV.Plot.SetAxisLimits(trend_dt[0], trend_dt[trend_data_item_max - 1], 0, 100);  // X軸の最小=0,X軸の最大=tred_data0の大きさ,Y軸最小=0, Y軸最大=100


            wpfPlot_Trend_MV.Plot.XAxis.Ticks(true, false, true);         // X軸の大きい目盛り=表示, X軸の小さい目盛り=非表示, X軸の目盛りのラベル=表示
            wpfPlot_Trend_MV.Plot.XAxis.TickLabelStyle(fontSize: 16);     //X軸   ラベルのフォントサイズ変更  :
            wpfPlot_Trend_MV.Plot.XAxis.TickLabelFormat("HH:mm:ss", dateTimeFormat: true); // X軸　時間の書式(例 12:30:15)、X軸の値は、日時型
            wpfPlot_Trend_MV.Plot.XLabel("time");                        // X軸全体のラベル

            wpfPlot_Trend_MV.Plot.YAxis.TickLabelStyle(fontSize: 16);    // Y軸   ラベルのフォントサイズ変更  :
            wpfPlot_Trend_MV.Plot.YLabel("[%]");


            trend_scatter_1.LineStyle = LineStyle.Dash;  // SVの線はDash
            trend_scatter_1.LineWidth = 3;               // SVの線を太くする。


            var legend1 = wpfPlot_Trend.Plot.Legend(enable: true, location: Alignment.UpperLeft);   // 凡例の表示
            var legend2 = wpfPlot_Trend_MV.Plot.Legend(enable: true, location: Alignment.UpperLeft);

            legend1.FontSize = 12;      // 凡例のフォントサイズ
            legend2.FontSize = 12;

        }


        // 定周期モニタ用
        // 
        private void SendIntervalTimer_Tick(object sender, EventArgs e)
        {
            bool fok = send_disp_data();       // データ送信
        }



        // 送信後、1000msec以内に受信文が得られないと、受信エラー
        //  
        private void RcvWaitTimer_Tick(object sender, EventArgs e)
        {

            RcvWaitTimer.Stop();        // 受信監視タイマの停止
            SendIntervalTimer.Stop();   // 定周期モニタ用タイマの停止
            ReStartTimer.Stop();        // 定周期モニタの再開用タイマの停止

            StatusTextBlock.Text = "Receive time out";
        }

        // 
        //  定周期モニタが停止した後の待ち時間後の処理
        //  (書き込みコマンドを送信する場合、定周期モニタを停止する。
        // 　定周期モニタ停止後、WriteWaitTimerが開始される。)
        private void WriteWaitTimer_Tick(object sender, EventArgs e)
        {
            WriteWaitTimer.Stop();      //　タイマ停止

            bool fok = send_disp_data();       // データ送信

            ReStartTimer.Start();              // 定周期モニタ開始待ちタイマの開始
        }

        // 書き込みコマンド送信後の、定周期モニタ開始待ち
        private void ReStartTimer_Tick(object sender, EventArgs e)
        {
            UInt16 crc_cd;

            ReStartTimer.Stop();                     //タイマ停止

            sendBuf[0] = 0x51;     // 送信コマンド  
            sendBuf[1] = 0;
            sendBuf[2] = 0;
            sendBuf[3] = 0;
            sendBuf[4] = 0;
            sendBuf[5] = 0;

            crc_cd = CRC_sendBuf_Cal(6);     // CRC計算

            sendBuf[6] = (Byte)(crc_cd >> 8); // CRCは上位バイト、下位バイトの順に送信
            sendBuf[7] = (Byte)(crc_cd & 0x00ff);

            sendByteLen = 8;                // 送信バイト数

            SendIntervalTimer.Start();     // データ収集用コマンド送信タイマー開始
        }

        // チェックボックスによるトレンド線の表示 (PV(ch1),SV,ch2,ch3,ch4,cjt 用)
        private void CH_N_Show(object sender, RoutedEventArgs e)
        {
            if (trend_scatter_0 is null) return;
            if (trend_scatter_1 is null) return;
            if (trend_scatter_2 is null) return;
            if (trend_scatter_3 is null) return;
            if (trend_scatter_4 is null) return;
            if (trend_scatter_5 is null) return;

            CheckBox checkBox = (CheckBox)sender;

            if (checkBox.Name == "PV_CheckBox")
            {
                trend_scatter_0.IsVisible = true;
            }
            else if (checkBox.Name == "SV_CheckBox")
            {
                trend_scatter_1.IsVisible = true;
            }
            else if (checkBox.Name == "Ch2_CheckBox")
            {
                trend_scatter_2.IsVisible = true;
            }
            else if (checkBox.Name == "Ch3_CheckBox")
            {
                trend_scatter_3.IsVisible = true;
            }
            else if (checkBox.Name == "Ch4_CheckBox")
            {
                trend_scatter_4.IsVisible = true;
            }

            else if (checkBox.Name == "Cjt_CheckBox")
            {
                trend_scatter_5.IsVisible = true;
            }

            wpfPlot_Trend.Render();   // グラフの更新

        }

        // チェックボックスによるトレンド線の非表示 (PV(ch1),SV,ch2,ch3,ch4,cjt 用)
        private void CH_N_Hide(object sender, RoutedEventArgs e)
        {

            if (trend_scatter_0 is null) return;
            if (trend_scatter_1 is null) return;
            if (trend_scatter_2 is null) return;
            if (trend_scatter_3 is null) return;
            if (trend_scatter_4 is null) return;
            if (trend_scatter_5 is null) return;

            CheckBox checkBox = (CheckBox)sender;

            if (checkBox.Name == "PV_CheckBox")
            {
                trend_scatter_0.IsVisible = false;
            }
            else if (checkBox.Name == "SV_CheckBox")
            {
                trend_scatter_1.IsVisible = false;
            }
            else if (checkBox.Name == "Ch2_CheckBox")
            {
                trend_scatter_2.IsVisible = false;
            }
            else if (checkBox.Name == "Ch3_CheckBox")
            {
                trend_scatter_3.IsVisible = false;
            }
            else if (checkBox.Name == "Ch4_CheckBox")
            {
                trend_scatter_4.IsVisible = false;
            }

            else if (checkBox.Name == "Cjt_CheckBox")
            {
                trend_scatter_5.IsVisible = false;
            }


            wpfPlot_Trend.Render();   // グラフの更新
        }

        // チェックボックスによるトレンド線の表示 (MV,P_MV,I_MV,D_MV,En 用)
        private void MV_N_Show(object sender, RoutedEventArgs e)
        {
            if (trend_scatter_6 is null) return;
            if (trend_scatter_7 is null) return;
            if (trend_scatter_8 is null) return;
            if (trend_scatter_9 is null) return;
            if (trend_scatter_10 is null) return;

            CheckBox checkBox = (CheckBox)sender;

            if (checkBox.Name == "MV_CheckBox")
            {
                trend_scatter_6.IsVisible = true;
            }
            else if (checkBox.Name == "P_MV_CheckBox")
            {
                trend_scatter_7.IsVisible = true;
            }
            else if (checkBox.Name == "I_MV_CheckBox")
            {
                trend_scatter_8.IsVisible = true;
            }
            else if (checkBox.Name == "D_MV_CheckBox")
            {
                trend_scatter_9.IsVisible = true;
            }
            else if (checkBox.Name == "En_CheckBox")
            {
                trend_scatter_10.IsVisible = true;
            }

            wpfPlot_Trend_MV.Render();   // グラフの更新

        }

        // チェックボックスによるトレンド線の表示 (MV,P_MV,I_MV,D_MV,En 用)
        private void MV_N_Hide(object sender, RoutedEventArgs e)
        {
            if (trend_scatter_6 is null) return;
            if (trend_scatter_7 is null) return;
            if (trend_scatter_8 is null) return;
            if (trend_scatter_9 is null) return;
            if (trend_scatter_10 is null) return;

            CheckBox checkBox = (CheckBox)sender;

            if (checkBox.Name == "MV_CheckBox")
            {
                trend_scatter_6.IsVisible = false;
            }
            else if (checkBox.Name == "P_MV_CheckBox")
            {
                trend_scatter_7.IsVisible = false;
            }
            else if (checkBox.Name == "I_MV_CheckBox")
            {
                trend_scatter_8.IsVisible = false;
            }
            else if (checkBox.Name == "D_MV_CheckBox")
            {
                trend_scatter_9.IsVisible = false;
            }
            else if (checkBox.Name == "En_CheckBox")
            {
                trend_scatter_10.IsVisible = false;
            }

            wpfPlot_Trend_MV.Render();   // グラフの更新
        }


        // チェックボックスによるヒストリトレンド線の表示 (PV(ch1),SV,ch2,ch3,ch4,cjt 用)
        private void History_CH_N_Show(object sender, RoutedEventArgs e)
        {
            if (history_scatter_0 is null) return;
            if (history_scatter_1 is null) return;
            if (history_scatter_2 is null) return;
            if (history_scatter_3 is null) return;
            if (history_scatter_4 is null) return;
            if (history_scatter_5 is null) return;

            CheckBox checkBox = (CheckBox)sender;

            if (checkBox.Name == "History_PV_CheckBox")
            {
                history_scatter_0.IsVisible = true;
            }
            else if (checkBox.Name == "History_SV_CheckBox")
            {
                history_scatter_1.IsVisible = true;
            }
            else if (checkBox.Name == "History_Ch2_CheckBox")
            {
                history_scatter_2.IsVisible = true;
            }
            else if (checkBox.Name == "History_Ch3_CheckBox")
            {
                history_scatter_3.IsVisible = true;
            }
            else if (checkBox.Name == "History_Ch4_CheckBox")
            {
                history_scatter_4.IsVisible = true;
            }

            else if (checkBox.Name == "History_Cjt_CheckBox")
            {
                history_scatter_5.IsVisible = true;
            }

            wpfPlot_History.Render();   // グラフの更新
        
        }
        // チェックボックスによるヒストリトレンド線の非表示 (PV(ch1),SV,ch2,ch3,ch4,cjt 用)
        private void History_CH_N_Hide(object sender, RoutedEventArgs e)
        {
            if (history_scatter_0 is null) return;
            if (history_scatter_1 is null) return;
            if (history_scatter_2 is null) return;
            if (history_scatter_3 is null) return;
            if (history_scatter_4 is null) return;
            if (history_scatter_5 is null) return;

            CheckBox checkBox = (CheckBox)sender;

            if (checkBox.Name == "History_PV_CheckBox")
            {
                history_scatter_0.IsVisible = false;
            }
            else if (checkBox.Name == "History_SV_CheckBox")
            {
                history_scatter_1.IsVisible = false;
            }
            else if (checkBox.Name == "History_Ch2_CheckBox")
            {
                history_scatter_2.IsVisible = false;
            }
            else if (checkBox.Name == "History_Ch3_CheckBox")
            {
                history_scatter_3.IsVisible = false;
            }
            else if (checkBox.Name == "History_Ch4_CheckBox")
            {
                history_scatter_4.IsVisible = false;
            }

            else if (checkBox.Name == "History_Cjt_CheckBox")
            {
                history_scatter_5.IsVisible = false;
            }

            wpfPlot_History.Render();   // グラフの更新

        }

        // チェックボックスによるヒストリトレンド線の表示 (MV,P_MV,I_MV,D_MV,En用)
        private void History_MV_N_Show(object sender, RoutedEventArgs e)
        {
            if (history_scatter_6 is null) return;
            if (history_scatter_7 is null) return;
            if (history_scatter_8 is null) return;
            if (history_scatter_9 is null) return;
            if (history_scatter_10 is null) return;

            CheckBox checkBox = (CheckBox)sender;
            
            if (checkBox.Name == "History_MV_CheckBox")
            {
                history_scatter_6.IsVisible = true;
            }
            else if (checkBox.Name == "History_P_MV_CheckBox")
            {
                history_scatter_7.IsVisible = true;
            }
            else if (checkBox.Name == "History_I_MV_CheckBox")
            {
                history_scatter_8.IsVisible = true;
            }
            else if (checkBox.Name == "History_D_MV_CheckBox")
            {
                history_scatter_9.IsVisible = true;
            }
            else if (checkBox.Name == "History_EN_CheckBox")
            {
                history_scatter_10.IsVisible = true;
            }

            wpfPlot_History_MV.Render();    // グラフの更新
        }

        // チェックボックスによるヒストリトレンド線の非表示 (MV,P_MV,I_MV,D_MV,En用)
        private void History_MV_N_Hide(object sender, RoutedEventArgs e)
        {
            if (history_scatter_6 is null) return;
            if (history_scatter_7 is null) return;
            if (history_scatter_8 is null) return;
            if (history_scatter_9 is null) return;
            if (history_scatter_10 is null) return;

            CheckBox checkBox = (CheckBox)sender;

            if (checkBox.Name == "History_MV_CheckBox")
            {
                history_scatter_6.IsVisible = false;
            }
            else if (checkBox.Name == "History_P_MV_CheckBox")
            {
                history_scatter_7.IsVisible = false;
            }
            else if (checkBox.Name == "History_I_MV_CheckBox")
            {
                history_scatter_8.IsVisible = false;
            }
            else if (checkBox.Name == "History_D_MV_CheckBox")
            {
                history_scatter_9.IsVisible = false;
            }
            else if (checkBox.Name == "History_EN_CheckBox")
            {
                history_scatter_10.IsVisible = false;
            }

            wpfPlot_History_MV.Render();    // グラフの更新
        }

        private delegate void DelegateFn();

        // データ受信時のイベント処理
        private void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            int id = System.Threading.Thread.CurrentThread.ManagedThreadId;
            Console.WriteLine("DataReceivedHandlerのスレッドID : " + id);

            int rd_num = ConfSerial.serialPort.BytesToRead;       // 受信データ数

            ConfSerial.serialPort.Read(rcvBuf, srcv_pt, rd_num);   // 受信データの読み出し

            srcv_pt = srcv_pt + rd_num;     // 次回の保存位置

            int rcv_total_byte = 0;
            if (rcvBuf[0] == 0x83)          // パラメータ書き込みコマンド(0x03)のレスポンスの場合
            {
                rcv_total_byte = 6;
            }
            else if (rcvBuf[0] ==0x84)           // モード変更コマンド(0x04)のレスポンスの場合
            {
                rcv_total_byte = 6;
            }
            else if (rcvBuf[0] == 0x85)           // MV変更コマンド(0x05)のレスポンスの場合
            {
                rcv_total_byte = 6;
            }

            else if (rcvBuf[0] == 0x90)           // フラッシュ書き込みコマンド(0x10)のレスポンスの場合
            {
                rcv_total_byte = 6;
            }

            else if (rcvBuf[0] == 0xd1)          // 温度パラメータモニタコマンド(0x51)のレスポンスの場合
            {
                rcv_total_byte = 46;
            }


            if (srcv_pt == rcv_total_byte)  // 最終データ受信済み 
            {
                RcvWaitTimer.Stop();        // 受信監視タイマー　停止

                receiveDateTime = DateTime.Now;   // 受信完了時刻を得る

                Dispatcher.BeginInvoke(new DelegateFn(RcvMsgProc)); // Delegateを生成して、RcvMsgProcを開始   (表示は別スレッドのため)
            }

        }


        //  
        //  最終データ受信後の処理
        private void RcvMsgProc()
        {

            if (rcvBuf[0] == 0xd1)     // 温度モニタコマンドのレスポンスの場合
            {
                Disp_monitor_data();   //  モニタ表示とグラフ表示
            }

            if ( CommLog.rcvframe_list != null)
            {
                CommLog.rcvmsg_disp();          // 受信データの表示       
            }
          
        }



        // モニタ画面表示とグラフ表示( 受信データから、PV等を取り出す)
        //
        //   受信データ :内容
        //     rcvBuf[0] : 0xd1 (コマンドに対するレスポンス)
        //     rcvBuf[1] :  mode_stop_run ( 0:Stop, 1:Run)
        //     rcvBuf[2] :  mode_auto_manual (0:Auto, 1:Manual)
        //     rcvBuf[3] : dummy 0
        //     rcvBuf[4] : PV(ch1)のLowバイト  (10倍した値 例:300ならば30.0℃を表す) 
        //     rcvBuf[5] :     :    Highバイト
        //     rcvBuf[6] : ch2 のLowバイト   (10倍した値 例:300ならば30.0℃を表す)
        //     rcvBuf[7] :  : のHighバイト   
        //     rcvBuf[8] : dummy 0
        //     rcvBuf[9] : dummy 0   
        //     rcvBuf[10] : dummy 0
        //     rcvBuf[11] : dummy 0 
        //     rcvBuf[12] : cjt のLowバイト  (10倍した値 例:150ならば15.0℃を表す)
        //     rcvBuf[13] :  : のHighバイト 
        //     rcvBuf[14] : MV のLowバイト   (10倍した値 例:100ならば10.0%を表す)  
        //     rcvBuf[15] :  : のHighバイト
        //     rcvBuf[16] : SV のLowバイト  (10倍した値 例:300ならば30.0℃を表す) 
        //     rcvBuf[17] :    のHighバイト
        //     rcvBuf[18] : P  のLowバイト ( 10倍した値 例:10ならば1%を表す)
        //     rcvBuf[19] :  : のHighバイト      
        //     rcvBuf[20] : I  のLowバイト
        //     rcvBuf[21] :  :  のHighバイト
        //     rcvBuf[22] : D  のLowバイト
        //     rcvBuf[23] :  : のHighバイト
        //     rcvBuf[24] : Mr のLowバイト  (10倍した値 (例:100ならば10.0%を表す)
        //     rcvBuf[25] :  : のHighバイト
        //     rcvBuf[26] : Hys のLowバイト  (10倍した値 (例:10ならば1.0℃を表す)
        //     rcvBuf[27] :  : のHighバイト    
        //     rcvBuf[28] : H/C (0:Heater(逆動作), 1:Coller(正動作))
        //     rcvBuf[29] : pid_type (未使用)
        //     rcvBuf[30] : pid_kg(ゲイン)  のLowバイト
        //     rcvBuf[31] :   :のHighバイト    
        //     rcvBuf[32] : En(偏差)のLowバイト (10倍した値 (例:10ならば1.0℃を表す)
        //     rcvBuf[33] :   :のHighバイト
        //     rcvBuf[34] : P_MVのLowバイト (10倍した値 (例:100ならば10.0%を表す)
        //     rcvBuf[35] :   :のHighバイト    
        //     rcvBuf[36] : I_MVのLowバイト (10倍した値 (例:100ならば10.0%を表す)
        //     rcvBuf[37] :   :のHighバイト          
        //     rcvBuf[38] : D_MVのLowバイト (10倍した値 (例:100ならば10.0%を表す)
        //     rcvBuf[39] :   :のHighバイト  
        //     rcvBuf[40] : ALM1
        //     rcvBuf[41] : ALM2
        //     rcvBuf[42] : ALM3
        //     rcvBuf[43] : ALM4
        //     rcvBuf[44] : CRC 上位バイト
        //     rcvBuf[45] : CRC 下位バイト
        //
        /// </summary>

        private void Disp_monitor_data()
        {
            float t;
            float ch1, ch2, ch3, ch4, cjt;
            float pv, sv, mv_out;
            float en, p_mv, i_mv, d_mv;

            Int16 dt;

            UInt16 crc_cd;

            crc_cd = CRC_rcvBuf_Cal(46);         // 全データのCRC計算             

            if ( crc_cd != 0)
            {
                Alarm_TextBox.Text = "Receive data CRC Err.";
                return;
            }

            mode_stop_run = rcvBuf[1];       //   mode_stop_run   ( 0:stop, 1:run ) 
            mode_auto_manual = rcvBuf[2];    //   mode_auto_manual   ( 0:auto, 1:manual ) 

            if (mode_stop_run == 0)
            {
                Mode_RUN_STOP_TextBox.Text = "stop";        // RUN/STOP表示
            }
            else if (mode_stop_run == 1)
            {
                Mode_RUN_STOP_TextBox.Text = "run";
            }

            if (mode_auto_manual == 0)
            {
                Mode_AUTO_MANUAL_TextBox.Text = "auto";        // Auto/Manual表示
                
            }
            else if (mode_auto_manual == 1)
            {
                Mode_AUTO_MANUAL_TextBox.Text = "manual";
               
            }

            if ((mode_stop_run == 1) && (mode_auto_manual == 1))    // RUNかつマニュアルの場合
            {
                MV_Input_TextBox.IsEnabled = true;              // MV変更ボタンの有効
                MV_Write_Button.IsEnabled = true;
            }
            else
            {
                MV_Input_TextBox.IsEnabled = false;              // MV変更ボタンの無効
                MV_Write_Button.IsEnabled = false;
            }


            dt = BitConverter.ToInt16(rcvBuf, 4);    // rcvBuf[4]から int16へ
            ch1 = (float)(dt / 10.0);
            pv = ch1;
            PV_CheckBoxTextBox.Text = pv.ToString("F1");   // PV(ch1)
            PV_TextBox.Text = pv.ToString("F1");

            dt = BitConverter.ToInt16(rcvBuf, 6);    // rcvBuf[6]から int16へ
            ch2 = (float)(dt / 10.0);
            Ch2_TextBox.Text = ch2.ToString("F1");   // ch2

            dt = BitConverter.ToInt16(rcvBuf, 8);    // rcvBuf[8]から int16へ
            ch3 = (float)(dt / 10.0);
           Ch3_TextBox.Text = ch3.ToString("F1") + "(invalid)";   // ch3 無効データ

            dt = BitConverter.ToInt16(rcvBuf, 10);    // rcvBuf[10]から int16へ
            ch4 = (float)(dt / 10.0);
            Ch4_TextBox.Text = ch4.ToString("F1")+ "(invalid)";   // ch4　無効データ

            dt = BitConverter.ToInt16(rcvBuf, 12);    // rcvBuf[12]から int16へ
            cjt = (float)(dt / 10.0);
            Cjt_TextBox.Text = cjt.ToString("F1");

            dt = BitConverter.ToInt16(rcvBuf, 14);    // rcvBuf[14]から int16へ
            mv_out = (float)(dt / 10.0);
            MV_TextBox.Text = mv_out.ToString("F1") + "%";
            MV_CheckBoxTextBox.Text = mv_out.ToString("F1") + "%";
        
            dt = BitConverter.ToInt16(rcvBuf, 16);    // rcvBuf[16]から int16へ
            sv = (float)(dt / 10.0);
            SV_TextBox.Text = sv.ToString("F1");
            SV_CheckBoxTextBox.Text = sv.ToString("F1");


            Para.Set_parameter_data();       // P,I,D等のパラメータ値を、paradata_listへ格納       


            Byte hc = rcvBuf[28];       // heat_cool
            string st = "";

            if (hc == 0)
            {
                st = "heater";
            }
            else if (hc == 1)
            {
                st = "cooler";
            }
            Para_Heat_Cool_TextBox.Text = st;      // Heater,Cooler表示


            dt = BitConverter.ToInt16(rcvBuf, 18);    // rcvBuf[18]から int16へ (P)
            if (dt == 0)        // P = 0の場合
            {
                st = "ON/OFF";
            }
            else                // P > 0の場合
            {
                st = "PID";
            }

            Para_PID_Type_TextBox.Text = st;     // PID　演算タイプの表示



            dt = BitConverter.ToInt16(rcvBuf, 32);    // rcvBuf[32]から int16へ
            en = (float)( dt / 10.0);
            En_TextBox.Text = en.ToString("F1");          // En表示

            dt = BitConverter.ToInt16(rcvBuf, 34);    // rcvBuf[34]から int16へ
            p_mv = (float)(dt / 10.0);
            P_MV_TextBox.Text = p_mv.ToString("F1");  // P_MV表示

            dt = BitConverter.ToInt16(rcvBuf, 36);    // rcvBuf[36]から int16へ
            i_mv = (float)(dt / 10.0);
            I_MV_TextBox.Text = i_mv.ToString("F1");  // I_MV表示

            dt = BitConverter.ToInt16(rcvBuf, 38);    // rcvBuf[38]から int16へ
            d_mv = (float)(dt / 10.0);
            D_MV_TextBox.Text = d_mv.ToString("F1");  // D_MV表示


            Array.Copy(trend_data0, 1, trend_data0, 0, trend_data_item_max - 1);
            trend_data0[trend_data_item_max - 1] = pv;

            Array.Copy(trend_data1, 1, trend_data1, 0, trend_data_item_max - 1);
            trend_data1[trend_data_item_max - 1] = sv;

            Array.Copy(trend_data2, 1, trend_data2, 0, trend_data_item_max - 1);
            trend_data2[trend_data_item_max - 1] = ch2;

            Array.Copy(trend_data3, 1, trend_data3, 0, trend_data_item_max - 1);
            trend_data3[trend_data_item_max - 1] = ch3;

            Array.Copy(trend_data4, 1, trend_data4, 0, trend_data_item_max - 1);
            trend_data4[trend_data_item_max - 1] = ch4;

            Array.Copy(trend_data5, 1, trend_data5, 0, trend_data_item_max - 1);
            trend_data5[trend_data_item_max - 1] = cjt;

            Array.Copy(trend_data6, 1, trend_data6, 0, trend_data_item_max - 1);
            trend_data6[trend_data_item_max - 1] = mv_out;

            Array.Copy(trend_data7, 1, trend_data7, 0, trend_data_item_max - 1);
            trend_data7[trend_data_item_max - 1] = p_mv;

            Array.Copy(trend_data8, 1, trend_data8, 0, trend_data_item_max - 1);
            trend_data8[trend_data_item_max - 1] = i_mv;

            Array.Copy(trend_data9, 1, trend_data9, 0, trend_data_item_max - 1);
            trend_data9[trend_data_item_max - 1] = d_mv;

            Array.Copy(trend_data10, 1, trend_data10, 0, trend_data_item_max - 1);
            trend_data10[trend_data_item_max - 1] = en;



            Array.Copy(trend_dt, 1, trend_dt, 0, trend_data_item_max - 1);
            trend_dt[trend_data_item_max - 1] = receiveDateTime.ToOADate();    // 受信日時 double型に変換して、格納

            wpfPlot_Trend.Render();   // リアルタイム グラフの更新
            wpfPlot_Trend_MV.Render();

            wpfPlot_Trend.Plot.AxisAuto();     // X軸の範囲を更新
            wpfPlot_Trend_MV.Plot.AxisAuto();


            HistoryData historyData = new HistoryData();     // 保存用ヒストリデータ

            historyData.data0 = pv;
            historyData.data1 = sv;
            historyData.data2 = ch2;
            historyData.data3 = ch3;
            historyData.data4 = ch4;
            historyData.data5 = cjt;
            historyData.data6 = mv_out;
          
            historyData.mode0 = mode_stop_run;
            historyData.mode1 = mode_auto_manual;

            historyData.data7 = p_mv;
            historyData.data8 = i_mv;
            historyData.data9 = d_mv;
            historyData.data10 = en;

            historyData.dt = receiveDateTime.ToOADate();   // 受信日時を deouble型で格納

            historyData_list.Add(historyData);      　　　　// Listへ保持
        }


        // CRCの計算 (受信バッファ用)
        //  CRC-16 CCITT:
        //  多項式: X^16 + X^12 + X^5 + 1　
        //  初期値: 0xffff
        //  MSBファースト
        //  非反転出力
        // 
        private UInt16 CRC_rcvBuf_Cal(UInt16 size)
        {
            UInt16 crc;

            UInt16 i;

            crc = 0xffff;

            for (i = 0; i < size; i++)
            {
                crc = (UInt16)((crc >> 8) | ((UInt16)((UInt32)crc << 8)));

                crc = (UInt16)(crc ^ rcvBuf[i]);
                crc = (UInt16)(crc ^ (UInt16)((crc & 0xff) >> 4));
                crc = (UInt16)(crc ^ (UInt16)((crc << 8) << 4));
                crc = (UInt16)(crc ^ (((crc & 0xff) << 4) << 1));
            }

            return crc;

        }


        // CRCの計算 (送信バッファ用)
        //  CRC-16 CCITT:
        //  多項式: X^16 + X^12 + X^5 + 1　
        //  初期値: 0xffff
        //  MSBファースト
        //  非反転出力
        // 
        public static UInt16 CRC_sendBuf_Cal( UInt16 size )
        {
            UInt16 crc;

            UInt16 i;

            crc = 0xffff;

            for (i = 0; i < size; i++)
            {
                crc = (UInt16)((crc >> 8) | ((UInt16)((UInt32)crc << 8)));

                crc = (UInt16)(crc ^ sendBuf[i]);
                crc = (UInt16)(crc ^ (UInt16)((crc & 0xff) >> 4));
                crc = (UInt16)(crc ^ (UInt16)((crc << 8) << 4));
                crc = (UInt16)(crc ^ (((crc & 0xff) << 4) << 1));
            }

            return crc;

        }


        // CRC計算のテスト
        //   0～9 までの　CRC値は 0xc241
        //   CRC値まで含めてCRCを計算すると、0になる。

        private void Test_CRC()
        {
            Byte i;
            UInt16 crc_cd;
            UInt16 crc_cd_ng;  // 0:CRC一致, 0以外:CRC不一致


            for (i = 0; i < 10; i++)
            {
                sendBuf[i] = i;
            }


            crc_cd = CRC_sendBuf_Cal(10);  // CRC値 0xc241

            //  sendBuf[10] = 0x41;
            //  sendBuf[11] = 0xc2;

              sendBuf[10] = 0xc2;
              sendBuf[11] = 0x41;

            crc_cd_ng = CRC_sendBuf_Cal(12);  // CRC値を含めてCRC計算 crc_cd_ng = 0 
        
        }



        // モニタ開始コマンド
        private void Start_Monitor_Button_Click(object sender, RoutedEventArgs e)
        {
            UInt16 crc_cd;

            sendBuf[0] = 0x51;     // 送信コマンド  
            sendBuf[1] = 0;
            sendBuf[2] = 0;
            sendBuf[3] = 0;
            sendBuf[4] = 0;
            sendBuf[5] = 0;

            crc_cd = CRC_sendBuf_Cal(6);     // CRC計算


            sendBuf[6] = (Byte)(crc_cd >> 8); // CRCは上位バイト、下位バイトの順に送信
            sendBuf[7] = (Byte)( crc_cd & 0x00ff );

            sendByteLen = 8;               // 送信バイト数

            send_msg_cnt = 0;              // 送信数 

            SendIntervalTimer.Start();   // 定周期　送信用タイマの開始

        }

        // コマンド送信の停止
        private void Stop_Monitor_Button_Click(object sender, RoutedEventArgs e)
        {
            SendIntervalTimer.Stop();     // データ収集用コマンド送信タイマー(1秒毎)停止
        }



        // STOP/Run ボタン
        // mode_stop_run  ( 0:stop, 1:run )
        // 動作モード(STOP/RUN)変更コマンドの発行
        private void Stop_Run_Button_Click(object sender, RoutedEventArgs e)
        {
            UInt16 crc_cd;

            SendIntervalTimer.Stop();     // データ収集用コマンド送信タイマー(1秒毎)停止
           
            Byte mode = 0;

            if (mode_stop_run == 0)     // 現在 stopの場合
            {
                mode = 1;               // runにする
            }
            else if (mode_stop_run == 1)　// 現在 runの場合
            {
                mode = 0;                 // stopにする
            }

            sendBuf[0] = 0x04;        // 送信コマンド  0x03 制御モード変更　コマンド　
            sendBuf[1] = mode;           //　stop/run モード変更
            sendBuf[2] = mode_auto_manual;
            sendBuf[3] = 0;
            sendBuf[4] = 0;
            sendBuf[5] = 0;

            crc_cd = CRC_sendBuf_Cal(6);     // CRC計算

            sendBuf[6] = (Byte)(crc_cd >> 8); // CRCは上位バイト、下位バイトの順に送信
            sendBuf[7] = (Byte)(crc_cd & 0x00ff);

            sendByteLen = 8;                   // 送信バイト数

            bool fok = send_disp_data();       // データ送信

            ReStartTimer.Start();    // 定周期モニタ開始待ちタイマの開始
        }

        //  Auto/Manual　ボタン
        //  Byte mode_auto_manual;  ( 0:auto, 1:manual	)
        //  制御モード(Auto/Manual)変更コマンドの発行
        private void Auto_Manual_Button_Click(object sender, RoutedEventArgs e)
        {
            UInt16 crc_cd;

            SendIntervalTimer.Stop();     // データ収集用コマンド送信タイマー(1秒毎)停止
     
            Byte mode = 0;

            if (mode_auto_manual == 0)     // 現在 autoの場合
            {
                mode = 1;                  // manualにする
            }
            else if (mode_auto_manual == 1)　// 現在 manualの場合
            {
                mode = 0;                 // autoにする
            }


            sendBuf[0] = 0x04;        // 送信コマンド  0x03 制御モード変更　コマンド　
            sendBuf[1] = mode_stop_run;     //　
            sendBuf[2] = mode;              // auto/manual変更
            sendBuf[3] = 0;
            sendBuf[4] = 0;
            sendBuf[5] = 0;

            crc_cd = CRC_sendBuf_Cal(6);     // CRC計算

            sendBuf[6] = (Byte)(crc_cd >> 8); // CRCは上位バイト、下位バイトの順に送信
            sendBuf[7] = (Byte)(crc_cd & 0x00ff);
        
            sendByteLen = 8;                   // 送信バイト数

            bool fok = send_disp_data();       // データ送信

            ReStartTimer.Start();    // 定周期モニタ開始待ちタイマの開始

        }
        // マニュアルモードでの出力設定
        private void MV_Write_Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                UInt16 crc_cd;

                SendIntervalTimer.Stop();     // データ収集用コマンド送信タイマー(1秒毎)停止

                float t = float.Parse(MV_Input_TextBox.Text); // 書き込みデータ (単精度浮動小数点型)

                byte[] byteArray = BitConverter.GetBytes(t);  // 書き込みデータをバイト配列へ変換
                                                              // t = 1.0の場合、byteArray[0]=0x00, byteArray[1]=0x00,byteArray[2]=0x80,byteArray[3]=0x3F
                                                              // lowバイト側から、配列へ格納されている。

                sendBuf[0] = 0x05;            　// 送信コマンド  0x05 MV値変更コマンド
                sendBuf[1] = 0;
                sendBuf[2] = byteArray[0];      //  単精度浮動小数点型データ 4byte
                sendBuf[3] = byteArray[1];
                sendBuf[4] = byteArray[2];
                sendBuf[5] = byteArray[3];

                crc_cd = CRC_sendBuf_Cal(6);     // CRC計算

                sendBuf[6] = (Byte)(crc_cd >> 8); // CRCは上位バイト、下位バイトの順に送信
                sendBuf[7] = (Byte)(crc_cd & 0x00ff);

                sendByteLen = 8;                   // 送信バイト数

                bool fok = send_disp_data();       // データ送信

                ReStartTimer.Start();              // 定周期モニタ開始待ちタイマの開始

            }
            catch (FormatException)
            {
                MV_Input_TextBox.Text = "Input Error";
            }
        }

        // フラッシュ書き込みボタン
        // PID用パラメータをRAMからE2データフラッシュへ書き込む
        private void Flash_Write_Button_Click(object sender, RoutedEventArgs e)
        {
            UInt16 crc_cd;

            SendIntervalTimer.Stop();     // データ収集用コマンド送信タイマー(1秒毎)停止

            sendBuf[0] = 0x10;            // 送信コマンド  0x10 フラッシュ書き込み　コマンド　

            sendBuf[1] = 0x00;
            sendBuf[2] = 0x00;
            sendBuf[3] = 0x00;
            sendBuf[4] = 0x00;
            sendBuf[5] = 0x00;

            crc_cd = CRC_sendBuf_Cal(6);     // CRC計算

            sendBuf[6] = (Byte)(crc_cd >> 8); // CRCは上位バイト、下位バイトの順に送信
            sendBuf[7] = (Byte)(crc_cd & 0x00ff);

            sendByteLen = 8;                   // 送信バイト数
            bool fok = send_disp_data();       // データ送信

            ReStartTimer.Start();    // 定周期モニタ開始待ちタイマの開始
        }


        //  送信と送信データの表示
        // sendBuf[]のデータを、sendByteLenバイト　送信する
        // 戻り値  送信成功時: true
        //         送信失敗時: false

        public  bool send_disp_data()
        {
            if (ConfSerial.serialPort.IsOpen == true)
            {
                srcv_pt = 0;                   // 受信データ格納位置クリア

                ConfSerial.serialPort.Write(sendBuf, 0, sendByteLen);     // データ送信

                send_msg_cnt++;              // 送信数インクリメント 

                if ( CommLog.sendframe_list != null)
                {
                    CommLog.sendmsg_disp();          // 送信データの表示
                }

                RcvWaitTimer.Start();        // 受信監視タイマー　開始

                StatusTextBlock.Text = "";
                return true;
            }

            else
            {
                StatusTextBlock.Text = "Comm port closed !";
                SendIntervalTimer.Stop();
                return false;
            }

        }



        //　通信条件の設定 ダイアログを開く
        //  
        private void Serial_Button_Click(object sender, RoutedEventArgs e)
        {
            var window = new ConfSerial();
            window.Owner = this;
            window.ShowDialog();

        }

        //
        //  保存ボタンの処理
        //
        private void Save_Button_Click(object sender, RoutedEventArgs e)
        {
            string path;

            string str_one_line;


            SaveFileDialog sfd = new SaveFileDialog();           //　SaveFileDialogクラスのインスタンスを作成 

            sfd.FileName = "ctl_trend.csv";                              //「ファイル名」で表示される文字列を指定する

            sfd.Title = "保存先のファイルを選択してください。";        //タイトルを設定する 

            sfd.RestoreDirectory = true;                 //ダイアログボックスを閉じる前に現在のディレクトリを復元するようにする

            if (sfd.ShowDialog() == true)            //ダイアログを表示する
            {
                path = sfd.FileName;

                try
                {
                    System.IO.StreamWriter sw = new System.IO.StreamWriter(path, false, System.Text.Encoding.Default);

                    str_one_line = DataMemoTextBox.Text; // メモ欄
                    sw.WriteLine(str_one_line);         // 1行保存

                    str_one_line = "";
                    foreach ( ParaData paraData in Para.paradata_list)    // パラメータ (SV,P,I,D等)を取り出す
                    {
                        str_one_line = str_one_line + paraData.name + "=" + paraData.st_dt + ",";
                    }
                    sw.WriteLine(str_one_line);         // パラメータを1行保存


                    str_one_line = "DateTime" + "," + "PV(ch1)" + "," + "SV" + "," + "MV" + "," + "ch2" + "," + "ch3(invalid)" + "," + "ch4(invalid)" + "," 
                                    + "cjt" + "," + "Stop/Run" + "," + "Auto/Manual" + "," + "P_MV" + "," + "I_MV" + "," + "D_MV" + "," + "En";

                    sw.WriteLine(str_one_line);         // 1行保存


                    foreach (HistoryData historyData in historyData_list)         // historyData_listの内容を保存
                    {
                        DateTime dateTime = DateTime.FromOADate(historyData.dt); // 記録されている日時(double型)を　DateTime型に変換

                        string st_dateTime = dateTime.ToString("yyyy/MM/dd HH:mm:ss.fff");             // DateTime型を文字型に変換　（2021/10/22 11:09:06.125 )
                     
                        string st_dt0 = historyData.data0.ToString("F1");       // PV(ch1) 文字型に変換 (25.0)
                        string st_dt1 = historyData.data1.ToString("F1");       // SV
                        string st_dt2 = historyData.data2.ToString("F1");       // ch2
                        string st_dt3 = historyData.data3.ToString("F1");       // ch3
                        string st_dt4 = historyData.data4.ToString("F1");       // ch4
                        string st_dt5 = historyData.data5.ToString("F1");       // cjt
                        string st_dt6 = historyData.data6.ToString("F1");       // MV

                        string st_mode0 ="";
                        if ( historyData.mode0 == 0)        // mode_stop_run = 0 の場合
                        {
                            st_mode0 = "Stop";
                        }
                        else
                        {
                            st_mode0 = "Run";
                        }

                        string st_mode1 = "";
                        if (historyData.mode1 == 0)        // mode_auto_manual = 0 の場合
                        {
                            st_mode1 = "Auto";
                        }
                        else
                        {
                            st_mode1 = "Manual";
                        }

                        string st_dt7 = historyData.data7.ToString("F1");       // P_MV
                        string st_dt8 = historyData.data8.ToString("F1");       // I_MV
                        string st_dt9 = historyData.data9.ToString("F1");       // D_MV
                        string st_dt10 = historyData.data10.ToString("F1");     // En

                        str_one_line = st_dateTime + "," + st_dt0 + "," + st_dt1 + "," + st_dt6 + "," + st_dt2 + "," + st_dt3 + "," + st_dt4 + "," + st_dt5 + "," + st_mode0 + ","
                                       + st_mode1 + "," + st_dt7 + "," + st_dt8 + "," + st_dt9 + "," + st_dt10;
                        

                        sw.WriteLine(str_one_line);         // 1行保存
                    }

                    sw.Close();
                }

                catch (System.Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }

            }
        }

        // Openボタンの処理
        // ヒストリデータファイルを読み出して、historyData_file_list へ格納
        private void Open_Button_Click(object sender, RoutedEventArgs e)
        {


            var dialog = new OpenFileDialog();   // ダイアログのインスタンスを生成

            dialog.Filter = "csvファイル (*.csv)|*.csv|全てのファイル (*.*)|*.*";  //  // ファイルの種類を設定

            dialog.RestoreDirectory = true;                 //ダイアログボックスを閉じる前に現在のディレクトリを復元するようにする


            if (dialog.ShowDialog() == false)     // ダイアログを表示する
            {
                return;                          // キャンセルの場合、リターン
            }


            try
            {
                historyData_file_list.Clear();            // ヒストリデータのクリア

                StreamReader sr = new StreamReader(dialog.FileName, Encoding.GetEncoding("SHIFT_JIS"));    //  CSVファイルを読みだし

                FileNameTextBox.Text = dialog.FileName;                // ファイル名の表示


                HistoryDataMemoTextBox.Text = sr.ReadLine();           // 先頭行の Memoを読み出し、表示

                sr.ReadLine();              // 2行目の、SV,P,I,D等のパラメータ 読み飛ばし
                
                sr.ReadLine();              // 読み飛ばし (3行目は、日時、ch名の項目名のため)

                while (!sr.EndOfStream)     // ファイル最終行まで、繰り返し
                {
                    HistoryData historyData = new HistoryData();        // 読み出しデータを格納するクラス

                    string line = sr.ReadLine();        // 1行の読み出し

                    string[] items = line.Split(',');       // 1行を、,(カンマ)毎に items[]に格納 

                    DateTime dateTime;
                    DateTime.TryParse(items[0], out dateTime);  // 日付の文字列を DateTime型へ変換

                    historyData.dt = dateTime.ToOADate();       // DateTiem型を double型へ変換


                    double.TryParse(items[1], out double d0); // PV(ch1)の値　文字列を double型へ変換
                    historyData.data0 = d0;                   // クラスのメンバーへ格納

                    double.TryParse(items[2], out double d1); // SVの値　文字列を double型へ変換
                    historyData.data1 = d1;                   // クラスのメンバーへ格納

                    double.TryParse(items[3], out double d6); // MVの値　文字列を double型へ変換
                    historyData.data6 = d6;

                    double.TryParse(items[4], out double d2); // ch2の値　文字列を double型へ変換
                    historyData.data2 = d2;                   // クラスのメンバーへ格納

                    double.TryParse(items[5], out double d3); // ch3の値　文字列を double型へ変換
                    historyData.data3 = d3;                   // クラスのメンバーへ格納

                    double.TryParse(items[6], out double d4); // ch4　文字列を double型へ変換
                    historyData.data4 = d4;                   // クラスのメンバーへ格納

                    double.TryParse(items[7], out double d5); // cjt　文字列を double型へ変換
                    historyData.data5 = d5;                   // クラスのメンバーへ格納

                    if (items[8] == "Stop")
                    {
                        historyData.mode0 = 0;
                    }
                    else if (items[8] == "Run")
                    {
                        historyData.mode0 = 1;
                    }

                    if (items[9] == "Auto")
                    {
                        historyData.mode1 = 0;
                    }
                    else if (items[8] == "Manual")
                    {
                        historyData.mode1 = 1;
                    }

                    double.TryParse(items[10], out double d7); // P_MV　文字列を double型へ変換
                    historyData.data7 = d7;                   // クラスのメンバーへ格納

                    double.TryParse(items[11], out double d8); // I_MV　文字列を double型へ変換
                    historyData.data8 = d8;                   // クラスのメンバーへ格

                    double.TryParse(items[12], out double d9); // D_MV　文字列を double型へ変換
                    historyData.data9 = d9;                   // クラスのメンバーへ格

                    double.TryParse(items[13], out double d10); // En　文字列を double型へ変換
                    historyData.data10 = d10;                   // クラスのメンバーへ格


                    historyData_file_list.Add(historyData);      // Listへ追加

                }

              
                disp_history_graph();       // ヒストリトレンドデータのグラフ表示
                set_check_box_true();       // チェックボックスをtrue
            }

            catch (Exception ex) when (ex is IOException || ex is IndexOutOfRangeException)
            {

                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);

            }
        }

        //  グラフデータのオープン時には、
        //  表示するグラフのチェックボックスを全てチェック済みにする。
        private void set_check_box_true()
        {
            History_PV_CheckBox.IsChecked = true;    
            History_SV_CheckBox.IsChecked = true;
            History_Ch2_CheckBox.IsChecked = true;

            History_Ch3_CheckBox.IsChecked = true;
            History_Ch4_CheckBox.IsChecked = true;

            History_Cjt_CheckBox.IsChecked = true;

            History_MV_CheckBox.IsChecked = true;
            History_P_MV_CheckBox.IsChecked = true;
            History_I_MV_CheckBox.IsChecked= true; ;
            History_D_MV_CheckBox.IsChecked = true;
            History_EN_CheckBox.IsChecked = true ;
        }


        //
        //  ヒストリトレンドデータのグラフ表示
        private void disp_history_graph()
        {
            wpfPlot_History.Plot.Clear();
            wpfPlot_History_MV.Plot.Clear();    

            int cnt_max = historyData_file_list.Count;       // 行数分の配列

            double[] t_data0 = new double[cnt_max];   // データ 0  PV
            double[] t_data1 = new double[cnt_max];   // データ 1  SV
            double[] t_data2 = new double[cnt_max];   // データ 2  ch2
            double[] t_data3 = new double[cnt_max];   // データ 3  ch3
            double[] t_data4 = new double[cnt_max];   // データ 4  ch4
            double[] t_data5 = new double[cnt_max];   // データ 5  cjt
            double[] t_data6 = new double[cnt_max];   // データ 6  MV
            double[] t_data7 = new double[cnt_max];   // データ 7  P_MV
            double[] t_data8 = new double[cnt_max];   // データ 8  I_MV
            double[] t_data9 = new double[cnt_max];   // データ 9  D_MV
            double[] t_data10 = new double[cnt_max];  // データ 10 En

            double[] t_dt = new double[cnt_max];       //  date time


            for (int i = 0; i < cnt_max; i++)                   // List化された、historyDataクラスの情報をグラフ表示用の配列にコピー 
            {
                t_data0[i] = historyData_file_list[i].data0;       // PV
                t_data1[i] = historyData_file_list[i].data1;       // SV
                t_data2[i] = historyData_file_list[i].data2;       // ch2
                t_data3[i] = historyData_file_list[i].data3;       // ch3 
                t_data4[i] = historyData_file_list[i].data4;       // ch4
                t_data5[i] = historyData_file_list[i].data5;       // cjt
                t_data6[i] = historyData_file_list[i].data6;       // MV
                t_data7[i] = historyData_file_list[i].data7;       // P_MV
                t_data8[i] = historyData_file_list[i].data8;       // I_MV
                t_data9[i] = historyData_file_list[i].data9;       // D_MV
                t_data10[i] = historyData_file_list[i].data10;     // En

                t_dt[i] = historyData_file_list[i].dt;           // data tiem
            }

            wpfPlot_History.Refresh();       // データ変更後は、Refresh
            wpfPlot_History_MV.Refresh();

            history_scatter_0 = wpfPlot_History.Plot.AddScatter(t_dt, t_data0, color: System.Drawing.Color.Blue, label: "PV(ch1)");    // 散布図へ表示　t_data0)
            history_scatter_1 = wpfPlot_History.Plot.AddScatter(t_dt, t_data1, color: System.Drawing.Color.Gainsboro, label: "SV");
            history_scatter_2 = wpfPlot_History.Plot.AddScatter(t_dt, t_data2, color: System.Drawing.Color.Orange, label: "ch2");
            history_scatter_3 = wpfPlot_History.Plot.AddScatter(t_dt, t_data3, color: System.Drawing.Color.Green, label: "ch3");     // ch3 無効データ
            history_scatter_4 = wpfPlot_History.Plot.AddScatter(t_dt, t_data4, color: System.Drawing.Color.SkyBlue, label: "ch4");   // ch4 無効データ
            history_scatter_5 = wpfPlot_History.Plot.AddScatter(t_dt, t_data5, color: System.Drawing.Color.Black, label: "cjt");

            history_scatter_6 = wpfPlot_History_MV.Plot.AddScatter(t_dt, t_data6, color: System.Drawing.Color.Red, label: "MV");
            history_scatter_7 = wpfPlot_History_MV.Plot.AddScatter(t_dt, t_data7, color: System.Drawing.Color.Pink, label: "P_MV");
            history_scatter_8 = wpfPlot_History_MV.Plot.AddScatter(t_dt, t_data8, color: System.Drawing.Color.DarkRed, label: "I_MV");
            history_scatter_9 = wpfPlot_History_MV.Plot.AddScatter(t_dt, t_data9, color: System.Drawing.Color.DarkOrange, label: "D_MV");
            history_scatter_10 = wpfPlot_History_MV.Plot.AddScatter(t_dt, t_data10, color: System.Drawing.Color.DarkSlateGray, label: "En");

            // PV History用
            wpfPlot_History.Configuration.Pan = true;               // パン(グラフの移動)　
            wpfPlot_History.Configuration.ScrollWheelZoom = true;   // ズーム(グラフの拡大、縮小)可

            wpfPlot_History.Plot.AxisAuto();                        // X軸、Y軸の上下限をデータに自動調整

            wpfPlot_History.Plot.XAxis.Ticks(true, false, true);         // X軸の大きい目盛り=表示, X軸の小さい目盛り=非表示, X軸の目盛りのラベル=表示
            wpfPlot_History.Plot.XAxis.DateTimeFormat(true);             // X軸の値は、日時型


            wpfPlot_History.Plot.XAxis.TickLabelStyle(fontSize: 16);     // X軸  Tickラベルのフォントサイズ変更  :
            wpfPlot_History.Plot.XLabel("time");                         // X軸全体のラベル

            wpfPlot_History.Plot.YAxis.TickLabelStyle(fontSize: 16);    // Y軸   ラベルのフォントサイズ変更  :
            wpfPlot_History.Plot.YLabel("[℃]");                         // Y軸全体のラベル

            var legend1 = wpfPlot_History.Plot.Legend(enable: true, location: Alignment.UpperLeft);   // 凡例の表示
            legend1.FontSize = 12;      // 凡例のフォントサイズ

            wpfPlot_History.Render();   // ヒストリデータ グラフの更新


            // MV グラフ
            wpfPlot_History_MV.Configuration.Pan = true;               // パン(グラフの移動)可
            wpfPlot_History_MV.Configuration.ScrollWheelZoom = true;   // ズーム(グラフの拡大、縮小)可

            wpfPlot_History_MV.Plot.AxisAutoX();                      // X軸の上下限は、表示するデータにより自動調整    
            wpfPlot_History_MV.Plot.SetAxisLimitsY(0.0, 100.0);       // Y軸の上下限は、0-100

            wpfPlot_History_MV.Plot.XAxis.Ticks(true, false, true);         // X軸の大きい目盛り=表示, X軸の小さい目盛り=非表示, X軸の目盛りのラベル=表示
            wpfPlot_History_MV.Plot.XAxis.DateTimeFormat(true);             // X軸の値は、日時型

            wpfPlot_History_MV.Plot.XAxis.TickLabelStyle(fontSize: 16);     //X軸   ラベルのフォントサイズ変更  :
           // wpfPlot_History_MV.Plot.XAxis.TickLabelFormat("HH:mm:ss", dateTimeFormat: true); // X軸　時間の書式(例 12:30:15)、X軸の値は、日時型
            wpfPlot_History_MV.Plot.XLabel("time");                        // X軸全体のラベル

            wpfPlot_History_MV.Plot.YAxis.TickLabelStyle(fontSize: 16);    // Y軸   ラベルのフォントサイズ変更  :
            wpfPlot_History_MV.Plot.YLabel("[%]");

            var legend2 = wpfPlot_History_MV.Plot.Legend(enable: true, location: Alignment.UpperLeft); // 凡例の表示 
            legend2.FontSize = 12;

            wpfPlot_History_MV.Render();   // ヒストリデータ グラフの更新
        }

            // 収集済みのデータをクリアの確認
        private void Clear_Button_Click(object sender, RoutedEventArgs e)
        {
            string messageBoxText = "収集済みのデータがクリアされます。";
            string caption = "Check clear";

            MessageBoxButton button = MessageBoxButton.YesNoCancel;
            MessageBoxImage icon = MessageBoxImage.Warning;
            MessageBoxResult result;

            result = MessageBox.Show(messageBoxText, caption, button, icon, MessageBoxResult.Yes);

            switch (result)
            {
                case MessageBoxResult.Yes:      // Yesを押した場合
                    historyData_list.Clear();   // 収集済みのデータのクリア
                    break;

                case MessageBoxResult.No: 
                    break;

                case MessageBoxResult.Cancel:
                    break;
            }


        }

        // パラメータ用 ウィンドウを開く
        private void Para_Button_Click(object sender, RoutedEventArgs e)
        {
                if (para_window_cnt > 0) return;   // 既に開いている場合、リターン

                var window = new Para();

                window.Owner = this;   // Paraウィンドウの親は、このMainWindow

                window.Show();

                para_window_cnt++;     // カウンタインクリメント
        }

        // 通信メッセージ表示用のウィンドウを開く
        private void Comm_Log_Button_Click(object sender, RoutedEventArgs e)
        {
            if (commlog_window_cnt > 0) return;   // 既に開いている場合、リターン

            var window = new CommLog();

            window.Owner = this;   // Paraウィンドウの親は、このMainWindow

            window.Show();

            commlog_window_cnt++;     // カウンタインクリメント
        }

        //　履歴データ表示画面へ移動するボタン
        private void History_Button_Click(object sender, RoutedEventArgs e)
        {
            TabSub1.IsSelected = true;            // TabSub1へ移動　(ヒストリデータのグラフ表示へ）
        }
        // モニタ表示画面へ移動するボタン
        private void Monitor_Button_Click(object sender, RoutedEventArgs e)
        {
            TabMain.IsSelected = true;          // TabMainへ移動
        }

      
    }
}
