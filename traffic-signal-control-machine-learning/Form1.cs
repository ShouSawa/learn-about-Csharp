using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Helpers;
using System.Web.UI.DataVisualization.Charting;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using static System.Net.Mime.MediaTypeNames;
using static System.Net.WebRequestMethods;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Rebar;

namespace 信号制御
{
    public partial class Form1 : Form
    {
        //Q学習のパラメータ
        //Qテーブル　11状態　×　100行動（縦x秒，横y秒でx,yは1～10）
        private double[,] northQtable = new double[11, 100]; //北のQテーブル
        private double[,] eastQtable = new double[11, 100]; //東のQテーブル
        private double[,] southQtable = new double[11, 100]; //南のQテーブル
        private double[,] westQtable = new double[11, 100]; //西のQテーブル

        //車のパラメータ
        private List<List<PictureBox>> cars; //車の画像たち　北[0,], 東[1,], 南[2,], 西[3,]
        private int[,] carState = new int[4, 12];//車の状態
        private int[,] direction = new int[4, 12];//車の向き
        private bool[,] use = new bool[4, 12]; //車が場に出ているかどうか

        private int s = 1; //Q学習をs倍する

        //列のパラメータ
        private bool[,] stop = new bool[4, 10]; //車が止まっているかどうか
        private PictureBox[,] stopCars = new PictureBox[4, 10]; //i,jに止まっている車

        Random random = new Random();
        int time = 0; //表示する経過時間

        //Q学習のパラメータ
        int episodeTime = 1000; //エピソードの繰り返し回数
        double testTime = 4.0; //試行回数

        double allgoalCount = 0.0; //累計収束回数
        double allActionCount = 0.0; // 累計行動実行回数
        double allRewardCount = 0.0; // 累計獲得報酬
        double allEfficientCount = 0.0; // 累計報酬獲得効率
        double allvarCount = 0; //縦の信号の方が青が長かった回数
        double allhorCount = 0; //横の信号の方が青が長かった回数
        double allequalCount = 0; //信号の長さが同じだった回数

        //報酬獲得効率のチャート
        double[] pointX = new double[1000];
        double[] pointY = new double[1000];

        //行動選択のチャート
        double[] pointX2 = new double[11]; //縦の信号が青になった秒数のカウント
        double[] pointY2 = new double[11];//横の信号が青になった秒数のカウント

        bool last = false;
        public Form1()
        {
            Debug.WriteLine("start");
            InitializeComponent();

            //交差点のサイズ設定
            intersection.Size = new Size(1000, 1000);

            for(int i = 0; i < testTime; i++)
            {
                Debug.WriteLine((i+1)+"回目の試行");
                if (i == testTime-1) last = true;
                QLeaning(); //Q学習スタート
                Debug.WriteLine("Q学習終了！");
                for(int j = 0; j < 4; j++)
                {
                    for(int k = 0; k < 10; k++)
                    {
                        stop[j,k] = false;
                    }
                }
            }


            chart1.ChartAreas.Clear();
            chart1.Series.Clear();

            // 「chartArea」という名前のエリアを生成します
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartAria = new System.Windows.Forms.DataVisualization.Charting.ChartArea("chartArea");

            // 生成したエリアをChartコントロールに追加します。
            chart1.ChartAreas.Add(chartAria);

            // Series(系列)を生成します
            System.Windows.Forms.DataVisualization.Charting.Series series = new System.Windows.Forms.DataVisualization.Charting.Series();

            // 系列の種類を折れ線グラフ(Line)に設定します
            series.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;

            // 系列の凡例を設置します
            series.LegendText = "報酬獲得効率";
            chart1.Titles.Add("報酬獲得効率の推移");
            chart1.ChartAreas[0].AxisX.Title = "エピソード数";
            chart1.ChartAreas[0].AxisY.Title = "報酬獲得効率";

            // 系列のポイント情報をセットします
            for (int i = 0; i < episodeTime; i++)
            {
                series.Points.AddXY(pointX[i], pointY[i]);
            }
            // 生成・設定した系列をChartコントロールに追加します
            chart1.Series.Add(series);


            chart2.ChartAreas.Clear();
            chart2.Series.Clear();
            chart2.Legends.Clear();
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartAria2 = new System.Windows.Forms.DataVisualization.Charting.ChartArea("chartArea");
            chart2.ChartAreas.Add(chartAria2);//グラフエリア
            chart2.Legends.Add(new System.Windows.Forms.DataVisualization.Charting.Legend());//凡例
            System.Windows.Forms.DataVisualization.Charting.Series series2 = new System.Windows.Forms.DataVisualization.Charting.Series();
            series2.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Bar;
            chart2.Titles.Add("縦の秒数の選択回数");
            chart2.ChartAreas[0].AxisX.Title = "縦の信号の青の秒数";
            chart2.ChartAreas[0].AxisY.Title = "回数";
            for (int i = 1; i < 11; i++)
            {
                series2.Points.AddY(pointX2[i]);
            }
            chart2.Series.Add(series2);

            chart3.ChartAreas.Clear();
            chart3.Series.Clear();
            chart3.Legends.Clear();
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartAria3 = new System.Windows.Forms.DataVisualization.Charting.ChartArea("chartArea");
            chart3.ChartAreas.Add(chartAria3);//グラフエリア
            chart3.Legends.Add(new System.Windows.Forms.DataVisualization.Charting.Legend());//凡例
            System.Windows.Forms.DataVisualization.Charting.Series series3 = new System.Windows.Forms.DataVisualization.Charting.Series();
            series3.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Bar;
            chart3.Titles.Add("横の秒数の選択回数");
            chart3.ChartAreas[0].AxisX.Title = "横の信号の青の秒数";
            chart3.ChartAreas[0].AxisY.Title = "回数";
            for (int i = 1; i < 11; i++)
            {
                series3.Points.AddY(pointY2[i]);
            }
            chart3.Series.Add(series3);
        }


        //Q学習-------------------------------------------------------------------

        int[] oldState = new int[4]; // 0：北，1：東，２：南，３；西
        int[] newState = new int[4]; // 0：北，1：東，２：南，３；西


        //Q学習本体
        double t = 0;
        private void QLeaning()
        {
            for (int i = 0; i < 10; i++)
            {
                pointX2[i] = 0;
                pointY2[i] = 0;
            }

            // ハイパーパラメータ
            double learningRate = 0.5;   // 学習率α
            double discountFactor = 0.7; // 割引率γ

            //Qテーブルの0埋め
            for (int i = 0; i < 11; i++)
            {
                for (int j = 0; j < 100; j++)
                {
                    northQtable[i, j] = 0.0;
                    eastQtable[i, j] = 0.0;
                    southQtable[i, j] = 0.0;
                    westQtable[i, j] = 0.0;
                }
            }

            double goalCount = 0.0; //累計収束回数
            double actionCount = 0.0; // 累計行動実行回数
            double rewardCount = 0.0; // 累計獲得報酬

            double varCount = 0.0;
            double horCount = 0.0;
            double equalCount = 0.0;
            t = 0;     

            // エピソードの繰りかえし
            for (int episode = 0; episode < episodeTime; episode++)
            {
                if(episode % 100 == 0)
                {
                    Debug.WriteLine((episode + 1) + "エピソード目");
                }
                
                //初期状態にリセット
                startIntersection();
                IsFinished = false;

                int count = 0;

                // アクションの繰りかえし
                while (!IsFinished)
                {
                    count++;
                    t += 0.1;
                    if (count == 1000)
                    {
                        //Debug.WriteLine("エピソード強制終了");
                        break; //1エピソード強制終了（Q学習してないときなどに使用）
                    }
                        //Debug.WriteLine(count + "アクション目");
                    for (int i = 0; i < 4; i++)
                    {
                        oldState[i] = QstateCheck(i); //Q値を更新したい状態
                    }

                    int[] action = choiceAction(); //行動aの決定（action[0]で縦信号，action[1]で横信号を青にする秒数を格納）

                    if (last)
                    {
                        pointX2[action[0]]++;
                        pointY2[action[1]]++;
                    }

                    if (action[0] > action[1])
                    {
                        varCount++;
                    }
                    else if (action[1] > action[0])
                    {
                        horCount++;
                    }
                    else
                    {
                        equalCount++;
                    }

                    doAction(action); //行動aの実行
                    

                    for (int i = 0; i < 4; i++)
                    {
                        newState[i] = QstateCheck(i); //Q値を更新したい状態の次の状態

                        //Q値の計算
                        if (i == 0)
                        {
                            /*
                            if (first == false)
                            {*/
                            northQtable[oldState[i], Qaction - 11] = (1 - learningRate) * northQtable[oldState[i], Qaction - 11] + learningRate * (rewardGet(newState[i]) + discountFactor * maxQvalueGet(i, newState[i]));
                            //}
                            //Debug.WriteLine("Q(" + oldState[i] + "," + Qaction + ")を" + northQtable[oldState[i], Qaction - 11] + "に更新");
                            oldState[i] = newState[i];
                            rewardCount += rewardGet(oldState[i]); // 累計獲得報酬に追加
                        }
                        else if (i == 1)
                        {
                            eastQtable[oldState[i], Qaction - 11] = (1 - learningRate) * eastQtable[oldState[i], Qaction - 11] + learningRate * (rewardGet(newState[i]) + discountFactor * maxQvalueGet(i, newState[i]));
                            //Debug.WriteLine("Q(" + oldState[i] + "," + Qaction + ")を" + eastQtable[oldState[i], Qaction - 11] + "に更新");
                            oldState[i] = newState[i];
                            rewardCount += rewardGet(oldState[i]); // 累計獲得報酬に追加
                        }
                        else if (i == 2)
                        {
                            southQtable[oldState[i], Qaction - 11] = (1 - learningRate) * southQtable[oldState[i], Qaction - 11] + learningRate * (rewardGet(newState[i]) + discountFactor * maxQvalueGet(i, newState[i]));
                            //Debug.WriteLine("Q(" + oldState[i] + "," + Qaction + ")を" + southQtable[oldState[i], Qaction - 11] + "に更新");
                            oldState[i] = newState[i];
                            rewardCount += rewardGet(oldState[i]); // 累計獲得報酬に追加
                        }
                        else if (i == 3)
                        {
                            westQtable[oldState[i], Qaction - 11] = (1 - learningRate) * westQtable[oldState[i], Qaction - 11] + learningRate * (rewardGet(newState[i]) + discountFactor * maxQvalueGet(i, newState[i]));
                            //Debug.WriteLine("Q(" + oldState[i] + "," + Qaction + ")を" + westQtable[oldState[i], Qaction - 11] + "に更新");
                            oldState[i] = newState[i];
                            rewardCount += rewardGet(oldState[i]); // 累計獲得報酬に追加
                        }
                    }
                    if (IsFinished) goalCount++;
                    actionCount++; // 累計行動実行回数をカウント
                }
                if (last)
                {
                    pointX[episode] = episode + 1;
                    pointY[episode] = rewardCount / actionCount;
                }
            }
            Debug.WriteLine("累計ゴール回数：" + goalCount);
            Debug.WriteLine("行動実行回数の平均 : " + (actionCount / 1000.0));
            Debug.WriteLine("獲得報酬の平均 : " + (rewardCount / 1000.0));
            Debug.WriteLine("報酬獲得効率 : " + (rewardCount / actionCount));
            allgoalCount += goalCount;
            allActionCount += (actionCount / 1000.0);
            allRewardCount += (rewardCount / 1000.0);
            allEfficientCount += (rewardCount / actionCount);
            allvarCount += (varCount / 1000.0);
            allhorCount += (horCount / 1000.0);
            allequalCount += (equalCount / 1000.0);
            if (last)
            {
                //Qview(0);
                //Qview(1);
                //Qview(2);
                //Qview(3);
                Debug.WriteLine("全体の累計ゴール回数の平均" );
                Debug.WriteLine("全体の行動実行回数の平均 ");
                Debug.WriteLine("全体の獲得報酬の平均 ");
                Debug.WriteLine("全体の報酬獲得効率の平均 ");

                Debug.WriteLine("縦の方が青が長かった回数の平均");
                Debug.WriteLine("横の方が青が長かった回数の平均");
                Debug.WriteLine("青の長さが縦横同じだった回数の平均");
                Debug.WriteLine((allgoalCount / testTime));
                Debug.WriteLine((allActionCount / testTime));
                Debug.WriteLine((allRewardCount / testTime));
                Debug.WriteLine((allEfficientCount / testTime));
                goalCountLabel.Text = (allgoalCount / testTime).ToString();
                actionCountLabel.Text = (allActionCount / testTime).ToString();
                rewardCountLabel.Text = (allRewardCount / testTime).ToString();
                label22.Text = (allEfficientCount / testTime).ToString();
                Debug.WriteLine(allvarCount / testTime);
                Debug.WriteLine(allhorCount / testTime);
                Debug.WriteLine(allequalCount / testTime);
            }
            
        }

        int northRate = 10; //北の車の溜まりやすさ(%)
        int southRate = 10; //南の車の溜まりやすさ
        int eastRate = 50; //東の車の溜まりやすさ
        int westRate = 10; //西の車の溜まりやすさ

        //初期配置
        private void startIntersection()
        {
            //北，南の方が溜まってる
            int ran = random.Next(10);
            for (int i = 0; i < ran; i++)
            {
                stop[0, i] = true;
            }
            ran = random.Next(10);
            for (int i = 0; i < ran; i++)
            {
                stop[1, i] = true;
            }
            ran = random.Next(10);
            for (int i = 0; i < ran; i++)
            {
                stop[2, i] = true;
            }
            ran = random.Next(10);
            for (int i = 0; i < ran; i++)
            {
                stop[3, i] = true;
            }
        }

        //現在の状態を取得
        private int QstateCheck(int i)
        {
            int countState = 0;
            for (int j = 0; j < 10; j++)
            {
                if (stop[i, j] == true) countState++; //車が何台止まっているかカウント
            }
            return countState;
        }

        //リワードを返す
        private double rewardGet(int s)
        {
           //return rewardCalc(s); //東西南北全てに別々の報酬を返す
            //return allRewardGet(); //東西南北全てに同じ報酬を返す（平均）
            return allRewardGet2(); //東西南北全てに同じ報酬を返す（足し算）
        }

        //全体のリワードを求めて返す
        private double allRewardGet()
        {
            double allReward = 0.0;
            allReward += rewardCalc(oldState[0]);
            allReward += rewardCalc(oldState[1]);
            allReward += rewardCalc(oldState[2]);
            allReward += rewardCalc(oldState[3]);
            allReward = allReward / 4; //平均を求める
            return allReward;
        }

        //全体のリワードを求めて返す
        private double allRewardGet2()
        {
            double allReward = 0.0;
            allReward += rewardCalc(oldState[0]);
            allReward += rewardCalc(oldState[1]);
            allReward += rewardCalc(oldState[2]);
            allReward += rewardCalc(oldState[3]);
            return allReward;
        }

        //リワードを計算
        private double rewardCalc(int s)
        {
            double reward = 0.0;
            for (int i = 0; i < s; i++) //車が溜まっていない程，リワードは10乗される．
            {
                if (s == 0) reward = 10000000000;
                if (s == 1) reward = 1000000000;
                if (s == 2) reward = 100000000;
                if (s == 3) reward = 10000000;
                if (s == 4) reward = 1000000;
                if (s == 5) reward = 100000;
                if (s == 6) reward = 10000;
                if (s == 7) reward = 1000;
                if (s == 8) reward = 100;
                if (s == 9) reward = 10;
                if (s == 10) reward = 0;
            }
            return reward;
        }

        //現在の状態の最大のQ値を返す
        private double maxQvalueGet(int a, int s)
    {
        double maxElement = 0.0; // 初期値を配列の最初の要素に設定
        if (a == 0) maxElement = northQtable[s, 0];
        if (a == 1) maxElement = eastQtable[s, 0];
        if (a == 2) maxElement = southQtable[s, 0];
        if (a == 3) maxElement = westQtable[s, 0];

        for (int i = 1; i < 100; i++)
        {
            // 配列の各要素と最大値を比較し、より大きい場合は最大値を更新
            if(a == 0)
            {
                if (northQtable[s, i] > maxElement)
                {
                    maxElement = northQtable[s, i];
                }
            }
            else if(a == 1)
            {
                if (eastQtable[s, i] > maxElement)
                {
                    maxElement = eastQtable[s, i];
                }
            }

            else if (a == 2)
            {
                if (southQtable[s, i] > maxElement)
                {
                    maxElement = southQtable[s, i];
                }
            }

            else if (a == 3)
            {
                if (westQtable[s, i] > maxElement)
                {
                    maxElement = westQtable[s, i];
                }
            }

            }
        return maxElement;
    }

        //現在の状態の最大のQ値の行動を返す
        private double maxActionGet(int a, int s)
        {
            double maxElement = 0.0; // 初期値を配列の最初の要素に設定
            int maxAction = 11;
            if (a == 0) maxElement = northQtable[s, 0];
            if (a == 1) maxElement = eastQtable[s, 0];
            if (a == 2) maxElement = southQtable[s, 0];
            if (a == 3) maxElement = westQtable[s, 0];

            for (int i = 1; i < 100; i++)
            {
                // 配列の各要素と最大値を比較し、より大きい場合は最大値を更新
                if (a == 0)
                {
                    if (northQtable[s, i] > maxElement)
                    {
                        maxElement = northQtable[s, i];
                        maxAction = i + 11;
                    }
                }
                else if (a == 1)
                {
                    if (eastQtable[s, i] > maxElement)
                    {
                        maxElement = eastQtable[s, i];
                        maxAction = i + 11;
                    }
                }

                else if (a == 2)
                {
                    if (southQtable[s, i] > maxElement)
                    {
                        maxElement = southQtable[s, i];
                        maxAction = i + 11;
                    }
                }

                else if (a == 3)
                {
                    if (westQtable[s, i] > maxElement)
                    {
                        maxElement = westQtable[s, i];
                        maxAction = i + 11;
                    }
                }

            }
            return maxAction;
        }


        //行動aの決定
        int Qaction = 11;
        private int[] choiceAction()
        {
            int[] action = new int[2];

            //行動aを固定
            //action[0] = 8;
            //action[1] = 8;

            //行動aをランダム
            //action = randomAction();


            //グリーディ
            action = greedy();

            //ボルツマン(失敗)
            //action = BoltzmannActionSelection();

            //以下のif文は，GUIで使う
            if (action[0] == 10)
            {
                varActionText.Text = "10";
            }
            else
            {
                if (action[0] != null)  varActionText.Text = action[0].ToString();
                
            }
            if (action[1] == 10)
            {
                horActionText.Text = "10";
            }
            else
            {
                if (action[1] != null)  horActionText.Text = action[1].ToString();
            }
            //Debug.WriteLine("行動a：" + action[0] + "," + action[1]);
            Qaction = action[0] * 10 + action[1];


            return action;
        }

        /*
        // ボルツマンの行動選択関数
        private int[] BoltzmannActionSelection()
        {
            int[] action = new int[2];
            int northAction = 0;
            int eastAction = 0;
            int southAction = 0;
            int westAction = 0;
            for (int j = 0; j < 4; j++)
            {
                Random r = new Random();
                double T = Math.Log(1 / (t + 1.1));
                double[] prob = new double[100];
                double total_act = 0;
                for (int i = 0; i < 100; i++)
                {
                    if (j == 0) total_act += Math.Exp(northQtable[oldState[j], i]);
                    if (j == 1) total_act += Math.Exp(eastQtable[oldState[j], i]);
                    if (j == 2) total_act += Math.Exp(southQtable[oldState[j], i]);
                    if (j == 3) total_act += Math.Exp(westQtable[oldState[j], i]);
                }
                for (int i = 0; i < 100; i++)
                {
                    if (j == 0) prob[i] = Math.Exp(northQtable[oldState[j], i] / T) / total_act;
                    if (j == 1) prob[i] = Math.Exp(eastQtable[oldState[j], i] / T) / total_act;
                    if (j == 2) prob[i] = Math.Exp(southQtable[oldState[j], i] / T) / total_act;
                    if (j == 3) prob[i] = Math.Exp(westQtable[oldState[j], i] / T) / total_act;
                }
                double rand = r.NextDouble();
                double cumulativeProbability = 0.0;

                for (int i = 0; i < 100; i++)
                {
                    cumulativeProbability += prob[i];
                    if (rand < cumulativeProbability)
                    {
                        if (j == 0)
                        {
                            northAction = i;
                            break;
                        }
                        if (j == 1)
                        {
                            eastAction = i; break;
                        }
                        if (j == 2)
                        {
                            southAction = i; break;
                        }
                        if (j == 3)
                        {
                            westAction = i; break;
                        }
                        
                    }
                }
            }

            //Q値が大きいものを行動として採用
            int bestAction = 0;
            if (northQtable[oldState[0], northAction] > eastQtable[oldState[1], eastAction])
            {
                bestAction = northAction;
            }
            else
            {
                bestAction = eastAction;
            }
            if (southQtable[oldState[2], southAction] > bestAction)
            {
                bestAction = southAction;
            }
            if (westQtable[oldState[3], westAction] > bestAction)
            {
                bestAction = westAction;
            }

            action[0] = bestAction / 10 + 1;
            action[1] = bestAction % 10 + 1;
            return action;
        }

        /*
        // ボルツマン分布の確率を計算
        private double[] CalculateBoltzmannProbabilities(double[] actionValues)
        {
            double[] probabilities = new double[100];
            double sum = actionValues.Sum(value => Math.Exp(value / t));

            for (int i = 0; i < 100; i++)
            {
                probabilities[i] = Math.Exp(actionValues[i] / t) / sum;
            }

            return probabilities;
        }*/

        //ランダムな行動aを返す
        private int[] randomAction()
        {
            int[] action = new int[2];
            int ran = random.Next(10) + 1; //1から10秒
            action[0] = ran;
            ran = random.Next(10) + 1; //1から10秒
            action[1] = ran;
            return action;
        }

        // ε-greedy法による行動の選択
        private int[] greedy()
        {
            int[] action = new int[2];
            int ran = random.Next(100);
            double epsilon = 0.3;
            if (ran < epsilon * 100) //30%でランダムな行動aを返す
            {
                //Debug.WriteLine("ランダムに行動を選択");
                return randomAction();
            }
            else
            {
               
                //Q値が大きいものを行動として採用
                int bestAction = 0;
                if (maxQvalueGet(0, oldState[0]) > maxQvalueGet(1, oldState[1])) 
                {
                    bestAction = getBestAction(0, oldState[0]) ;
                }
                else
                {
                    bestAction = getBestAction(1, oldState[1]) ;
                }

                if (maxQvalueGet(2, oldState[2]) > bestAction)
                {
                    bestAction = getBestAction(2, oldState[2]) ;
                }

                if (maxQvalueGet(3, oldState[3]) > bestAction)
                {
                    bestAction = getBestAction(3, oldState[3]) ;
                }
                action[0] = bestAction/10 + 1;
                action[1] = bestAction % 10 + 1;
                //Debug.WriteLine("Q値を参考に行動を選択");


                /*
               //縦，横それぞれで，Q値が大きいものを行動として採用
               if (maxQvalueGet(0, oldState[0]) > maxQvalueGet(2, oldState[2]))
                               {
                                   action[0] = getBestAction(0, oldState[0]) / 10 +1;
                               }
                               else
                               {
                                   action[0] = getBestAction(2, oldState[2]) / 10 + 1;
                               }
                               if (maxQvalueGet(1, oldState[1]) > maxQvalueGet(3, oldState[3]))
                               {
                                   action[1] = getBestAction(1, oldState[1]) / 10 + 1;
                               }
                               else
                               {
                                   action[1] = getBestAction(3, oldState[3]) / 10 + 1;
                               }
                               */
                return action;
            }
        }

        // Qテーブルから最もQ値が高い行動aを取得
        private int getBestAction(int a, int state)
        {
            double maxQValue = 0.0;
            if (a == 0) maxQValue = northQtable[state, 0];
            if (a == 1) maxQValue = eastQtable[state, 0];
            if (a == 2) maxQValue = southQtable[state, 0];
            if (a == 3) maxQValue = westQtable[state, 0];

            int bestAction = 0;

            for(int i = 0; i < 100; i++)
            {
                if (a == 0)
                {
                    if (northQtable[state, i] > maxQValue)
                    {
                        maxQValue = northQtable[state, i];
                        bestAction = i;
                    }
                } 
                else if (a == 1)
                {
                    if (eastQtable[state, i] > maxQValue)
                    {
                        maxQValue = eastQtable[state, i];
                        bestAction = i;
                    }
                }
                else if (a == 2)
                {
                    if (southQtable[state, i] > maxQValue)
                    {
                        maxQValue = southQtable[state, i];
                        bestAction = i;
                    }
                }
                else if (a == 3)
                {
                    if (westQtable[state, i] > maxQValue)
                    {
                        maxQValue = westQtable[state, i];
                        bestAction = i;
                    }
                }
            }

            return bestAction;
        }

        //行動aの実行
        int addCarCount = 0;
        private void doAction(int[] action)
        {
            addCarCount = 0;
            int[] actionTime = action; //アクションの決定，動作時間

            //縦信号が青　以下，並列で実行
            drive(0,actionTime[0]);//北の車の行動開始
            drive(2,actionTime[0]);//南の車の行動開始
            QpackCheck();
            if (rightCheck(0) == false && rightCheck(1) == false && rightCheck(2) == false && rightCheck(3) == false)
            {
                IsFinished = true;
                return; //どこにも車がいなくなってたら，1エピソード終了
            }
            QaddCar(actionTime[0]);

            //横信号が青 以下，並列で実行
            drive(1,actionTime[1]);//東の車の行動開始
            drive(3,actionTime[1]);//西の車の行動開始
            packCheck();
            if (rightCheck(0) == false && rightCheck(1) == false && rightCheck(2) == false && rightCheck(3) == false)
            {
                IsFinished = true;
                return;
            }
            QaddCar(actionTime[1]);
        }

        //車が運転する i=0:北,1:東,2:南,3:西
        bool[] wantRightTrun = {false, false, false, false}; //iの車が右折したがってるかどうかを格納
        private async Task drive(int i,int actionTime)
        {
            while (true)
            {
                wantRightTrun[i] = false;
                if (rightCheck(0) == false && rightCheck(1) == false && rightCheck(2) == false && rightCheck(3) == false)
                {
                    IsFinished = true; 
                    return; //どこにも車がいなくなってたら，1エピソード終了
                }
                if (stop[i, 0] == true) //iに車があるとき
                {
                    if (getDriveRate() == 0) //直進のとき
                    {
                        actionTime -= 1; //1秒消費
                        stop[i, 0] = false;
                        Qpack(i);
                    }
                    else if (getDriveRate() == 2) //左折のとき
                    {
                        if (actionTime == 1) 
                        {
                            return; //残り1秒で信号が赤になるときは，曲がらない
                        }
                        actionTime -= 2; //2秒消費
                        stop[i, 0] = false;
                        Qpack(i);
                    }
                    else if (getDriveRate() == 1) //右折のとき
                    {
                        wantRightTrun[i] = true;
                        if (actionTime == 1)
                        {
                            return; //残り1秒で信号が赤になるときは，曲がらない
                        }
                        while (true)
                        {
                            //互いの反対の列を見る
                            int a = 0; 
                            if (i == 0) a=2;
                            if (i == 1) a=3;
                            if (i == 2) a=0;
                            if (i == 3) a=1;

                            //向かいの列に車がいない，あるいは，向かいの列の車も右折しようとしている場合は，右折して良し
                            if (rightCheck(a) || (wantRightTrun[i] == true && wantRightTrun[a] == true))
                            {
                                actionTime -= 2; //2秒消費
                                stop[i, 0] = false;
                                Qpack(i);
                                if (actionTime <= 1)
                                {
                                    return; //残り1秒で信号が赤になるときは，曲がらない
                                }
                            }
                            else
                            {
                                actionTime -= 1; //右折せずに1秒待つ
                                if (actionTime <= 1)
                                {
                                    return; //残り1秒で信号が赤になるときは，曲がらない
                                } 
                            }
                        }
                    }

                }
                else //iの列に車がないとき
                {
                    actionTime -= 1; //1秒待つ
                }

                if (actionTime <= 0) return; //信号が変わったら戻る
            }
        }

        //車が直進，右折，左折，どの運転をするのかを決定する
        private int getDriveRate()
        {
            int rate = random.Next(1, 101); //1から100までの乱数を生成
            int driveAction = 0;

            if (rate < 33)
            {
                driveAction = 0;
            }
            else if (rate < 66)
            {
                driveAction = 1;
            }
            else
            {
                driveAction = 2;
            }

            return driveAction; //直進：0，右折：1，左折：２を返却
        }

        //aの列の車を詰める 0:北，1:東，2:南，3:西
        private void Qpack(int a)
        {
            if (stop[a, 1] == true)
            {
                for (int i = 1; i < 10; i++)
                {
                    if (stop[a, i] == true)
                    {
                        stop[a, i - 1] = true;
                        stop[a, i] = false;
                    }
                    else
                    {
                        stop[a, i - 1] = false;
                        break;
                    }

                }
                stop[a, 0] = true;
            }
        }

        //右折できるかどうかチェック
        private bool rightCheck(int a)
        {
            for (int i = 0; i < 10; i++)
            {
                if (stop[a, i] == true)
                {
                    return true;
                }
            }
            return false;
        }

        //車が詰めてるかどうかを検査，詰まってなかったら詰める
        private async Task QpackCheck()
        {
            for (int a = 0; a < 4; a++)
            {
                for (int i = 1; i < 10; i++)
                {
                    if (stop[a, i - 1] == false && stop[a, i] == true)
                    {
                        stop[a, i - 1] = true;
                        stop[a, i] = false;
                    }
                }
            }
        }

        //追加で来る車の設定（環境によって変化）
        private async Task QaddCar(int actionTime)
        {
            for (int k = 0; k < actionTime; k++)
            {
                for (int i = 0; i < 4; i++)
                {
                    if (addCheck(i)) //北に車が追加できるとき
                    {
                        if (i == 0)
                        {
                            if (random.Next(1, 101) < northRate) //毎秒random%の確率で車が追加
                            {
                                QaddSet(i);
                                addCarCount++;
                            }
                        }
                        else if (i == 2)
                        {
                            if (random.Next(1, 101) < southRate) //毎秒ransom%の確率で車が追加
                            {
                                QaddSet(i);
                                addCarCount++;
                            }
                        }
                        else if (i == 1)
                        {
                            if (random.Next(1, 101) < eastRate) //毎秒ransom%の確率で車が追加
                            {
                                QaddSet(i);
                                addCarCount++;
                            }
                        }
                        else if (i == 3)
                        {
                            if (random.Next(1, 101) < westRate) //毎秒ransom%の確率で車が追加
                            {
                                QaddSet(i);
                                addCarCount++;
                            }
                        }
                    }
                }
                if (IsFinished)
                {
                    return;
                }
            }

        }

        //車が追加できるかどうかチェック
        private bool addCheck(int a)
        {
            for (int i = 0; i < 10; i++)
            {
                if (stop[a, i] == false)
                {
                    return true;
                }
            }
            return false;
        }

        //追加で車を止める，aで東西南北を指定（北南東西，0123）
        private async void QaddSet(int a)
        {
            int r = 0;
            for (int j = 0; j < 10; j++)
            {
                if (stop[a, j] == false)
                {
                    r = j;
                    break;
                }
            }
            stop[a, r] = true;
        }

        //Qテーブルの表示
        private void Qview(int a)
        {
            if (a == 0) Debug.WriteLine("northQtable");
            if (a == 1) Debug.WriteLine("eastQtable");
            if (a == 2) Debug.WriteLine("southQtable");
            if (a == 3) Debug.WriteLine("westQtable");
            Debug.Write("   | ");
            for (int i = 1; i <= 10; i++)
            {
                for (int j = 1; j <= 10; j++)
                {

                    string str = i.ToString() + j.ToString();
                    Debug.Write(String.Format("{0, 13}", str));
                    Debug.Write(" | ");
                }

            }

            Debug.WriteLine("");
            for (int j = 1; j < 102; j++)
            {
                Debug.Write("---------------");
            }
            Debug.WriteLine("");
            for (int i = 0; i < 11; i++)
            {
                Debug.Write(String.Format("{0, 2}", i));
                Debug.Write(" |");
                for (int j = 0; j < 100; j++)
                {
                    if (a == 0) Debug.Write($" {string.Format("{0, 13}", Math.Round(northQtable[i, j], 2))}");
                    if (a == 1) Debug.Write($" {string.Format("{0, 13}", Math.Round(eastQtable[i, j], 2))}");
                    if (a == 2) Debug.Write($" {string.Format("{0, 13}", Math.Round(southQtable[i, j], 2))}");
                    if (a == 3) Debug.Write($" {string.Format("{0, 13}", Math.Round(westQtable[i, j], 2))}");
                    Debug.Write(" |");
                }
                Debug.WriteLine("");
            }
        }

        //以下，GUI----------------------------------------------------------------------------
        //Q学習スタートボタン
        //パラメータのチェック
        private void parametersCheck()
        {
            //列のパラメータ
            Debug.WriteLine("stop");
            for (int i = 0; i < 4; i++)
            {
                for(int j = 0; j < 10; j++)
                {
                    Debug.WriteLine("stop[" + i + ","+j+"]=" +stop[i,j]);
                }
            }
            Debug.WriteLine("stopCars");
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 10; j++)
                {
                    if (stopCars[i,j] == null)
                        Debug.WriteLine("null");
                }
            }
            //車のパラメータ
            Debug.WriteLine("carState");
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 12; j++)
                {
                        Debug.WriteLine("CarState[" + i + "," + j + "]=" + carState[i, j]);
                }
            }
            Debug.WriteLine("direction");
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 12; j++)
                {
                        Debug.WriteLine("direction[" + i + "," + j + "]=" + direction[i, j]);
                }
            }
            Debug.WriteLine("use");
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 12; j++)
                {
                    Debug.WriteLine("use[" + i + "," + j + "]=" + use[i, j]);
                }
            }
        }

        //タイマーのインターバルをセット（s倍）
        private void timerIntervalSet(int s)
        {
            northCar1_go.Interval = 100 / s;
            northCar2_timer.Interval = 100 / s;
            northCar3_timer.Interval = 100 / s;
            northCar4_timer.Interval = 100 / s;
            northCar5_timer.Interval = 100 / s;
            northCar6_timer.Interval = 100 / s;
            northCar7_timer.Interval = 100 / s;
            northCar8_timer.Interval = 100 / s;
            northCar9_timer.Interval = 100 / s;
            northCar10_timer.Interval = 100 / s;
            northCar11_timer.Interval = 100 / s;
            northCar12_timer.Interval = 100 / s;
            eastCar1_timer.Interval = 100 / s;
            eastCar2_timer.Interval = 100 / s;
            eastCar3_timer.Interval = 100 / s;
            eastCar4_timer.Interval = 100 / s;
            eastCar5_timer.Interval = 100 / s;
            eastCar6_timer.Interval = 100 / s;
            eastCar7_timer.Interval = 100 / s;
            eastCar8_timer.Interval = 100 / s;
            eastCar9_timer.Interval = 100 / s;
            eastCar10_timer.Interval = 100 / s;
            eastCar11_timer.Interval = 100 / s;
            eastCar12_timer.Interval = 100 / s;
            southCar1_timer.Interval = 100 / s;
            southCar2_timer.Interval = 100 / s;
            southCar3_timer.Interval = 100 / s;
            southCar4_timer.Interval = 100 / s;
            southCar5_timer.Interval = 100 / s;
            southCar6_timer.Interval = 100 / s;
            southCar7_timer.Interval = 100 / s;
            southCar8_timer.Interval = 100 / s;
            southCar9_timer.Interval = 100 / s;
            southCar10_timer.Interval = 100 / s;
            southCar11_timer.Interval = 100 / s;
            southCar12_timer.Interval = 100 / s;
            westCar1_timer.Interval = 100 / s;
            westCar2_timer.Interval = 100 / s;
            westCar3_timer.Interval = 100 / s;
            westCar4_timer.Interval = 100 / s;
            westCar5_timer.Interval = 100 / s;
            westCar6_timer.Interval = 100 / s;
            westCar7_timer.Interval = 100 / s;
            westCar8_timer.Interval = 100 / s;
            westCar9_timer.Interval = 100 / s;
            westCar10_timer.Interval = 100 / s;
            westCar11_timer.Interval = 100 / s;
            westCar12_timer.Interval = 100 / s;
        }
       
        int count = 0; //何エピソード目か
        private async void Qstart_Click(object sender, EventArgs e)
        {
            timerIntervalSet(s);
            for (int i = 1; i < 10; i++){
                count++;
                episodeText.Text = i.ToString();
                await oneEpisode();
            }
        }

        //1エピソード
        bool IsFinished = false; //1エピソードが終わったかどうか
        int actionCount = 0; //何アクション目か
        string check = "";
        bool first = true;//1回目のQテーブル生成であるか
        private async Task oneEpisode()
        {
            check = "OUT";
            actionCount = 0;
            reset(); // 初期状態にリセット
            startIntersection();
            startset();
            check = "OK";
            IsFinished = false;
            while(!IsFinished)
            {
                stateCheck(); //Q値を更新したい状態を獲得
                actionCount++;
                ActionText.Text = actionCount.ToString();
                await oneAction();
                first = false;
                /*
                Qview(0);
                Qview(1);
                Qview(2);
                Qview(3);
                */
            }
            Debug.WriteLine("第" + count + "エピソード終了！");
        }

        //状態の抽出
        private void stateCheck()
        {
            for (int i = 0; i < 4; i++)
            {
                int countState = 0;
                for (int j = 0; j < 10; j++)
                {
                    if (stop[i, j] == true) countState++;
                }
                oldState[i] = countState;
                if (i == 0)
                {
                    if (count != 10)
                    {
                        northStateText.Text = countState.ToString();
                    }
                    else
                    {
                        northStateText.Text = "10";
                    }

                }
                else if (i == 1)
                {
                    if (count != 10)
                    {
                        eastStateText.Text = countState.ToString();
                    }
                    else
                    {
                        eastStateText.Text = "10";
                    }
                    
                }
                else if (i == 2)
                {
                    if (count != 10)
                    {
                        southStateText.Text = countState.ToString();
                    }
                    else
                    {
                        southStateText.Text = "10";
                    }

                    
                }
                else if (i == 3)
                {
                    if (count != 10)
                    {
                        westStateText.Text = countState.ToString();
                    }
                    else
                    {
                        westStateText.Text = "10";
                    }
                    
                }
            }
        }
        
        //1回の行動
        private async Task oneAction()
        {
            //縦信号が青
            int[] actionTime = choiceAction(); //アクションの決定，動作時間
            lightVer(true);
            time = 0;
            timer.Enabled = true; //タイマー起動
            northDrive(actionTime[0]);//北の車の行動開始
            southDrive(actionTime[0]);//南の車の行動開始
            
            packCheck();
            if (rightCheck(0) == false && rightCheck(1) == false && rightCheck(2) == false && rightCheck(3) == false)
            {
                IsFinished = true;
                return;
            }
            addCar(actionTime[0]);
            await Task.Delay(actionTime[0] * 1000 / s);

            //横信号が青
            lightVer(false);
            time = 0;
            timer.Enabled = true; //タイマー起動

            eastDrive(actionTime[1]);//東の車の行動開始
            westDrive(actionTime[1]);//西の車の行動開始
            
            packCheck();
            if (rightCheck(0) == false && rightCheck(1) == false && rightCheck(2) == false && rightCheck(3) == false)
            {
                IsFinished = true;
                return;
            }
            addCar(actionTime[1]);
            await Task.Delay(actionTime[1] * 1000/s);
        }

        //パラメータのリセット
        private void reset()
        {
            //全タイマーストップ
            for (int i = 0; i < 4; i++)
            {
                for(int j = 0; j < 12; j++)
                {
                    control(cars[i][j],false);
                }
            }

            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 12; j++)
                {
                    cars[i][j].Parent = intersection; //全車の透過
                    if (i == 0 || i == 2) cars[i][j].Size = new Size(25, 40);//全車のサイズ設定
                    if (i == 1 || i == 3) cars[i][j].Size = new Size(40, 25);
                }
            }
            
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 12; j++)
                {
                    Bitmap bmp = (Bitmap)cars[i][j].Image;
                    if (i == 0)
                    {
                        if (direction[i, j] == 90) bmp.RotateFlip(RotateFlipType.Rotate90FlipNone);
                        if (direction[i, j] == 0) bmp.RotateFlip(RotateFlipType.Rotate180FlipNone);
                        if (direction[i, j] == 270) bmp.RotateFlip(RotateFlipType.Rotate270FlipNone);
                        cars[i][j].Image = bmp;
                        carState[i, j] = 0;
                        direction[i, j] = 180;
                    }
                    if (i == 1)
                    {
                        if (direction[i, j] == 90) bmp.RotateFlip(RotateFlipType.Rotate180FlipNone);
                        if (direction[i, j] == 0) bmp.RotateFlip(RotateFlipType.Rotate270FlipNone);
                        if (direction[i, j] == 180) bmp.RotateFlip(RotateFlipType.Rotate90FlipNone);
                        cars[i][j].Image = bmp;
                        carState[i, j] = 2;
                        direction[i, j] = 270;
                    }
                    if (i == 2)
                    {
                        if (direction[i, j] == 90) bmp.RotateFlip(RotateFlipType.Rotate270FlipNone);
                        if (direction[i, j] == 270) bmp.RotateFlip(RotateFlipType.Rotate90FlipNone);
                        if (direction[i, j] == 180) bmp.RotateFlip(RotateFlipType.Rotate180FlipNone);
                        carState[i, j] = 3;
                        direction[i, j] = 0;

                    }
                    if (i == 3)
                    {
                        if (direction[i, j] == 180) bmp.RotateFlip(RotateFlipType.Rotate270FlipNone);
                        if (direction[i, j] == 0) bmp.RotateFlip(RotateFlipType.Rotate90FlipNone);
                        if (direction[i, j] == 270) bmp.RotateFlip(RotateFlipType.Rotate180FlipNone);
                        carState[i, j] = 1;
                        direction[i, j] = 90;

                    }
                    use[i, j] = false;
                }
            }

            for (int i = 0; i < 4; ++i)
            {
                for (int j = 0; j < 10; j++)
                {
                    stop[i, j] = false;
                }
            }


            //最初に車を全て非表示
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 12; j++)
                {
                    cars[i][j].Visible = false; //非表示
                }
            }

            
        }

        //環境///////////////////////////////////////////////////////////
        //初期準備
        private void Form1_Load(object sender, EventArgs e)
        {
            // 2次元の配列に画像を配置
            cars = new List<List<PictureBox>>
            {
                new List<PictureBox> { northCar1, northCar2, northCar3,northCar4,northCar5,northCar6,northCar7,northCar8,northCar9,northCar10,northCar11,northCar12},
                new List<PictureBox>{eastCar1,eastCar2,eastCar3,eastCar4,eastCar5,eastCar6,eastCar7,eastCar8,eastCar9,eastCar10,eastCar11,eastCar12},
                new List<PictureBox>{southCar1,southCar2,southCar3,southCar4,southCar5,southCar6,southCar7,southCar8,southCar9,southCar10,southCar11,southCar12},
                new List<PictureBox>{westCar1, westCar2, westCar3, westCar4, westCar5, westCar6, westCar7, westCar8, westCar9, westCar10, westCar11, westCar12 }
            };

            //信号
            lightBlue1.Size = new Size(40, 20);
            lightBlue2.Size = new Size(20, 40);
            lightBlue3.Size = new Size(40, 20);
            lightBlue4.Size = new Size(20, 40);
            Bitmap bmp2 = (Bitmap)lightBlue2.Image;
            bmp2.RotateFlip(RotateFlipType.Rotate90FlipX);
            bmp2 = (Bitmap)lightBlue4.Image;
            bmp2.RotateFlip(RotateFlipType.Rotate270FlipX);
            lightBlue1.Top = 405; lightBlue1.Left = 465;
            lightBlue2.Top = 475; lightBlue2.Left = 575;
            lightBlue3.Top = 565; lightBlue3.Left = 500;
            lightBlue4.Top = 515; lightBlue4.Left = 405;
            lightRed1.Size = new Size(40, 20);
            lightRed2.Size = new Size(20, 40);
            lightRed3.Size = new Size(40, 20);
            lightRed4.Size = new Size(20, 40);
            bmp2 = (Bitmap)lightRed2.Image;
            bmp2.RotateFlip(RotateFlipType.Rotate90FlipX);
            bmp2 = (Bitmap)lightRed4.Image;
            bmp2.RotateFlip(RotateFlipType.Rotate270FlipX);
            lightRed1.Top = 405; lightRed1.Left = 465;
            lightRed2.Top = 475; lightRed2.Left = 575;
            lightRed3.Top = 565; lightRed3.Left = 500;
            lightRed4.Top = 515; lightRed4.Left = 405;

        }

        //車の初期配置
        private void startset()　//止める場所をstop[,] = trueにしている
        {
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 10; j++)
                {
                    if (stop[i, j] == true) //もしi,jに止める必要があれば，
                    {
                        for (int l = 0; l < 12; l++)
                        {
                            if (use[i, l] == false) //使っていない車を止める
                            {
                                use[i, l] = true;
                                stop[i, j] = true;
                                carSet(i, j, cars[i][l]);
                                stopCars[i, j] = cars[i][l];//停める車を記録
                                break;
                            }
                        }
                    }
                }
            }
        }

        //車が直進，右折，左折，どの運転をするのかを決定する
        private int getDriveRate(PictureBox pictureBox)
        {
            int rate = random.Next(1,101); //1から100までの乱数を生成
            int driveAction = 0;

            if(rate < 33)
            {
                driveAction = 0;
            }
            else if(rate < 66)
            {
                driveAction = 1;
            }
            else
            {
                driveAction = 2;
            }

            return driveAction; //直進：0，右折：1，左折：２を返却
        }

        //追加で来る車の設定（環境によって変化）
        private async Task addCar(int actionTime)
        {
            for(int k = 0; k < actionTime; k++)
            {
                await Task.Delay(1000/s);
                for (int i = 0; i < 4; i++)
                {
                    if (addCheck(i)) //北に車が追加できるとき
                    {
                        if(i == 0)
                        {
                            if (random.Next(1, 101) < northRate) //毎秒15%の確率で車が追加
                            {
                                addSet(i);
                            }
                        }
                        else if (i == 2)
                        {
                            if (random.Next(1, 101) < southRate) //毎秒5%の確率で車が追加
                            {
                                addSet(i);
                            }
                        }
                        else if (i == 1)
                        {
                            if (random.Next(1, 101) < eastRate) //毎秒5%の確率で車が追加
                            {
                                addSet(i);
                            }
                        }
                        else if (i == 3)
                        {
                            if (random.Next(1, 101) < westRate) //毎秒5%の確率で車が追加
                            {
                                addSet(i);
                            }
                        }

                    }
                }
                if (IsFinished)
                {
                    return;
                }
            }

        }

        //追加で車を止める，aで東西南北を指定（北南東西，0123）
        private async void addSet(int a)
        {
            int r = 0;
            for(int j = 0; j < 10; j++)
            {
                if(stop[a, j] == false)
                {
                    r = j; 
                    break;
                }
            }
            int delay = 0;
            if (r == 0) delay = 800;
            if (r == 1) delay = 700;
            if (r == 2) delay =  600;
            if (r == 3) delay = 500;
            if (r == 4) delay = 400;
            if (r == 5) delay = 400;
            if (r == 6) delay =  300;
            if (r == 7) delay =  200;
            if (r == 8) delay = 100;

            for (int l = 0; l < 12; l++)
            {
                if (use[a, l] == false) //使っていない車を止める
                {
                    if (a == 0)
                    {
                        cars[a][l].Size = new Size(25, 40);
                        cars[a][l].Left = 505;
                        cars[a][l].Top = 10;
                        cars[a][l].Visible = true;
                        use[a, l] = true;
                        carState[a,l] = 0;
                        //direction[a, l] = 180;
                        control(cars[a][l], true);
                        await Task.Delay(delay/s);
                        control(cars[a][l], false);
                        packCheck();
                    }
                    else if (a == 1)
                    {
                        cars[a][l].Size = new Size(40,25);
                        cars[a][l].Left = 990;
                        cars[a][l].Top = 507;
                        cars[a][l].Visible = true;
                        carState[a, l] = 2;
                        use[a, l] = true;
                        control(cars[a][l], true);
                        await Task.Delay(delay/s);
                        control(cars[a][l], false);
                        packCheck();
                    }
                    else if (a == 2)
                    {
                        cars[a][l].Size = new Size(25, 40);
                        cars[a][l].Left = 470;
                        cars[a][l].Top = 990;
                        cars[a][l].Visible = true;
                        carState[a, l] = 3;
                        use[a, l] = true;
                        //direction[a, l] = 0;
                        control(cars[a][l], true);
                        await Task.Delay(delay/s);
                        control(cars[a][l], false);
                        packCheck();
                    }
                    else if (a == 3)
                    {
                        cars[a][l].Size = new Size(40, 25);
                        cars[a][l].Left = 10;
                        cars[a][l].Top = 475;
                        cars[a][l].Visible = true;
                        carState[a, l] = 1;
                        use[a, l] = true;
                        //direction[a, l] = 90;
                        control(cars[a][l], true);
                        await Task.Delay(delay/s);
                        control(cars[a][l], false);
                        packCheck();
                    }

                    use[a, l] = true;
                    stop[a, r] = true;
                    carSet(a, r, cars[a][l]);
                    stopCars[a, r] = cars[a][l];//停める車を記録
                    break;
                }
            }
        }

        //テスト
        private void redSet_Click(object sender, EventArgs e)
        {

            stop[0, 0] = false;
            startset();
        }

        private void moveTest_Click(object sender, EventArgs e)
        {
            addSet(0);
        }

        //車の動作/////////////////////////////////////////////////////////
        
        //詰めてるかどうかを検査
        private async Task packCheck()
        {
            await Task.Delay(400/s);
            for (int a = 0; a < 4; a++)
            {
                for (int i = 1; i < 10; i++)
                {
                    if (stop[a, i - 1] == false && stop[a, i] == true)
                    {
                        stop[a, i - 1] = true;
                        stop[a, i] = false;
                        stopCars[a, i - 1] = stopCars[a, i];
                        stopCars[a, i] = null;
                        carSet(a, i - 1, stopCars[a, i - 1]);
                    }
                }
            }
        }

        bool rightTurn1 = false;
        bool rightTurn2 = false;
        //北の車の1行動中に行う運転
        private async Task northDrive(int actionTime)
        {
            while (true)
            {
                rightTurn1 = false;
                if (rightCheck(0) == false && rightCheck(1) == false && rightCheck(2) == false && rightCheck(3) == false)
                {
                    IsFinished = true;
                    return;
                }
                if (stop[0, 0] == true) //北に車があるとき
                {
                    if (getDriveRate(stopCars[0, 0]) == 0) //直進のとき
                    {
                        straight(stopCars[0, 0]);
                        await Task.Delay(500 / s);
                        actionTime -= 1;
                        await Task.Delay(500 / s);
                    }
                    else if (getDriveRate(stopCars[0, 0]) == 2) //左折のとき
                    {
                        if(actionTime == 1)
                        {
                            return;
                        }
                        leftTrun(stopCars[0, 0]);
                        await Task.Delay(2000 / s);
                        actionTime -= 2;
                    }
                    else if (getDriveRate(stopCars[0, 0]) == 1) //右折のとき
                    {
                        rightTurn1 = true;
                        if (actionTime == 1)
                        {
                            return;
                        }
                        while (true)
                        {
                            //互いに右折しようとしている場合は，右折して良し
                            if (rightCheck(2) && !(rightTurn1 == true && rightTurn2 == true))
                            {
                                await Task.Delay(1000 / s);
                                actionTime -= 1;
                                if (actionTime <= 1)
                                {
                                    return;
                                }
                            }
                            else
                            {
                                rightTrun(stopCars[0, 0]);
                                await Task.Delay(2000 / s);
                                actionTime -= 2;
                                if (actionTime <= 1)
                                {
                                    return;
                                }
                            }
                        }
                    }

                }
                else //北に車がないとき
                {
                    await Task.Delay(1000 / s);
                    actionTime -= 1;
                }

                if (actionTime <= 0) return;
            }
        }

        //南の車の1行動中に行う運転
        private async Task southDrive(int actionTime)
        {
            while (true)
            {
                rightTurn2 = false;
                if (rightCheck(0) == false && rightCheck(1) == false && rightCheck(2) == false && rightCheck(3) == false)
                {
                    IsFinished = true;
                    return;
                }
                if (stop[2, 0] == true) //南に車があるとき
                {
                    if (getDriveRate(stopCars[2, 0]) == 0)
                    {
                        straight(stopCars[2, 0]);
                        await Task.Delay(500 / s);
                        actionTime -= 1;
                        await Task.Delay(500 / s);
                    }
                    else if (getDriveRate(stopCars[2, 0]) == 2) //左折のとき
                    {
                        if (actionTime == 1)
                        {
                            return;
                        }
                        leftTrun(stopCars[2, 0]);
                        await Task.Delay(2000 / s);
                        actionTime -= 2;
                    }
                    else if (getDriveRate(stopCars[2, 0]) == 1) //右折のとき
                    {
                        rightTurn2 = true;
                        if (actionTime == 1)
                        {
                            return;
                        }
                        while (true)
                        {
                            if (rightCheck(0) && !(rightTurn1 == true && rightTurn2 == true))
                            {
                                await Task.Delay(1000 / s);
                                actionTime -= 1;
                                if (actionTime <= 1)
                                {
                                    return;
                                }
                            }
                            else
                            {
                                rightTrun(stopCars[2, 0]);
                                await Task.Delay(2000 / s);
                                actionTime -= 2;
                                if (actionTime <= 1)
                                {
                                    return;
                                }
                            }
                        }
                    }
                }
                else //南に車がないとき
                {
                    await Task.Delay(1000 / s);
                    actionTime -= 1;
                }
                

                if (actionTime <= 0) return;
            }
        }

        //東の車の1行動中に行う運転
        private async Task eastDrive(int actionTime)
        {
            while (true)
            {
                rightTurn1 = false;
                if (rightCheck(0) == false && rightCheck(1) == false && rightCheck(2) == false && rightCheck(3) == false)
                {
                    IsFinished = true;
                    return;
                }
                if (stop[1, 0] == true) //東に車があるとき
                {
                    if (getDriveRate(stopCars[1, 0]) == 0)
                    {
                        straight(stopCars[1, 0]);
                        await Task.Delay(500 / s);
                        actionTime -= 1;
                        await Task.Delay(500 / s);
                    }
                    else if (getDriveRate(stopCars[1, 0]) == 2) //左折のとき
                    {
                        if (actionTime == 1)
                        {
                            return;
                        }
                        leftTrun(stopCars[1, 0]);
                        await Task.Delay(2000 / s);
                        actionTime -= 2;
                    }
                    else if (getDriveRate(stopCars[1, 0]) == 1) //右折のとき
                    {
                        rightTurn1 = true;
                        if (actionTime <= 1)
                        {
                            return;
                        }
                        while (true)
                        {
                            if (rightCheck(3) && !(rightTurn1 == true && rightTurn2 == true))
                            {
                                await Task.Delay(1000 / s);
                                actionTime -= 1;
                                if (actionTime <= 1)
                                {
                                    return;
                                }
                            }
                            else
                            {
                                rightTrun(stopCars[1, 0]);
                                await Task.Delay(2000 / s);
                                actionTime -= 2;
                                if (actionTime <= 1)
                                {
                                    return;
                                }
                            }
                        }
                    }
                }
                else //東に車がないとき
                {
                    await Task.Delay(1000 / s);
                    actionTime -= 1;
                }
                

                if (actionTime <= 0) return;
            }
        }

        //西の車の1行動中に行う運転
        private async Task westDrive(int actionTime)
        {
            while (true)
            {
                rightTurn2 = false;
                if (rightCheck(0) == false && rightCheck(1) == false && rightCheck(2) == false && rightCheck(3) == false)
                {
                    IsFinished = true;
                    return;
                }
                if (stop[3, 0] == true) //西に車があるとき
                {
                    if (getDriveRate(stopCars[3, 0]) == 0)
                    {
                        straight(stopCars[3, 0]);
                        await Task.Delay(500 / s);
                        actionTime -= 1;
                        await Task.Delay(500 / s);
                    }
                    else if (getDriveRate(stopCars[3, 0]) == 2) //左折のとき
                    {
                        if (actionTime == 1)
                        {
                            return;
                        }
                        leftTrun(stopCars[3, 0]);
                        await Task.Delay(2000 / s);
                        actionTime -= 2;
                    }
                    else if (getDriveRate(stopCars[3, 0]) == 1) //右折のとき
                    {
                        rightTurn2 = true;
                        if (actionTime <= 1)
                        {
                            return;
                        }
                        while (true)
                        {
                            if (rightCheck(1) && !(rightTurn1 == true && rightTurn2 == true))
                            {
                                await Task.Delay(1000 / s);
                                actionTime -= 1;
                                if (actionTime <= 1)
                                {
                                    return;
                                }
                            }
                            else
                            {
                                rightTrun(stopCars[3, 0]);
                                await Task.Delay(2000 / s);
                                actionTime -= 2;
                                if (actionTime <= 1)
                                {
                                    return;
                                }
                            }
                        }
                    }
                }
                else //西sに車がないとき
                {
                    await Task.Delay(1000 / s);
                    actionTime -= 1;
                }
                

                if (actionTime <= 0) return;
            }
        }

        //各車のタイマーを起動(pictureBoxの車をgo = trueで動かすor止める , carStateで車の動作を指定)
        private async void control(PictureBox pictureBox, bool go)
        {
            if (pictureBox == northCar1) northCar1_go.Enabled = go;
            if (pictureBox == northCar2) northCar2_timer.Enabled = go;
            if (pictureBox == northCar3) northCar3_timer.Enabled = go;
            if (pictureBox == northCar4) northCar4_timer.Enabled = go;
            if (pictureBox == northCar5) northCar5_timer.Enabled = go;
            if (pictureBox == northCar6) northCar6_timer.Enabled = go;
            if (pictureBox == northCar7) northCar7_timer.Enabled = go;
            if (pictureBox == northCar8) northCar8_timer.Enabled = go;
            if (pictureBox == northCar9) northCar9_timer.Enabled = go;
            if (pictureBox == northCar10) northCar10_timer.Enabled = go;
            if (pictureBox == northCar11) northCar11_timer.Enabled = go;
            if (pictureBox == northCar12) northCar12_timer.Enabled = go;
            if (pictureBox == eastCar1) eastCar1_timer.Enabled = go;
            if (pictureBox == eastCar2) eastCar2_timer.Enabled = go;
            if (pictureBox == eastCar3) eastCar3_timer.Enabled = go;
            if (pictureBox == eastCar4) eastCar4_timer.Enabled = go;
            if (pictureBox == eastCar5) eastCar5_timer.Enabled = go;
            if (pictureBox == eastCar6) eastCar6_timer.Enabled = go;
            if (pictureBox == eastCar7) eastCar7_timer.Enabled = go;
            if (pictureBox == eastCar8) eastCar8_timer.Enabled = go;
            if (pictureBox == eastCar9) eastCar9_timer.Enabled = go;
            if (pictureBox == eastCar10) eastCar10_timer.Enabled = go;
            if (pictureBox == eastCar11) eastCar11_timer.Enabled = go;
            if (pictureBox == eastCar12) eastCar12_timer.Enabled = go;
            if (pictureBox == southCar1) southCar1_timer.Enabled = go;
            if (pictureBox == southCar2) southCar2_timer.Enabled = go;
            if (pictureBox == southCar3) southCar3_timer.Enabled = go;
            if (pictureBox == southCar4) southCar4_timer.Enabled = go;
            if (pictureBox == southCar5) southCar5_timer.Enabled = go;
            if (pictureBox == southCar6) southCar6_timer.Enabled = go;
            if (pictureBox == southCar7) southCar7_timer.Enabled = go;
            if (pictureBox == southCar8) southCar8_timer.Enabled = go;
            if (pictureBox == southCar9) southCar9_timer.Enabled = go;
            if (pictureBox == southCar10) southCar10_timer.Enabled = go;
            if (pictureBox == southCar11) southCar11_timer.Enabled = go;
            if (pictureBox == southCar12) southCar12_timer.Enabled = go;
            if (pictureBox == westCar1) westCar1_timer.Enabled = go;
            if (pictureBox == westCar2) westCar2_timer.Enabled = go;
            if (pictureBox == westCar3) westCar3_timer.Enabled = go;
            if (pictureBox == westCar4) westCar4_timer.Enabled = go;
            if (pictureBox == westCar5) westCar5_timer.Enabled = go;
            if (pictureBox == westCar6) westCar6_timer.Enabled = go;
            if (pictureBox == westCar7) westCar7_timer.Enabled = go;
            if (pictureBox == westCar8) westCar8_timer.Enabled = go;
            if (pictureBox == westCar9) westCar9_timer.Enabled = go;
            if (pictureBox == westCar10) westCar10_timer.Enabled = go;
            if (pictureBox == westCar11) westCar11_timer.Enabled = go;
            if (pictureBox == westCar12) westCar12_timer.Enabled = go;
        }
        
        //車の配置（止まっているpictureBox参照）
        private void set()
        {
            for(int i = 0;i < 4;i++)
            {
                for( int j = 0;j < 10;j++)
                {
                    if (stop[i, j] == true)
                    {
                        carSet(i, j, stopCars[i, j]);//stopCarsに入っている情報を元に配置
                    }
                }
            }
        }

        private void oneActionButton_Click(object sender, EventArgs e)
        {
            oneAction();
        }

        //タイマー
        private void timer_Tick(object sender, EventArgs e)
        {
            timer.Interval = 1000 / s;
            time += 1;
            timerLabel.Text = time.ToString();

        }

        //車の配置の表示
        private void carSet(int i, int j, PictureBox pictureBox)
        {
            if(pictureBox == null)
            {
                getName(pictureBox);
                return;
            }
            cars[i][j].Visible = true;
            if (i == 0) //北
            {
                pictureBox.Left = 505;
                if (j == 0) pictureBox.Top = 377;
                if (j == 1) pictureBox.Top = 337;
                if (j == 2) pictureBox.Top = 297;
                if (j == 3) pictureBox.Top = 257;
                if (j == 4) pictureBox.Top = 217;
                if (j == 5) pictureBox.Top = 177;
                if (j == 6) pictureBox.Top = 137;
                if (j == 7) pictureBox.Top = 97;
                if (j == 8) pictureBox.Top = 57;
                if (j == 9) pictureBox.Top = 17;
            }
            else if (i == 2) //南
            {
                pictureBox.Left = 470;
                if (j == 0) pictureBox.Top = 583;
                if (j == 1) pictureBox.Top = 628;
                if (j == 2) pictureBox.Top = 668;
                if (j == 3) pictureBox.Top = 708;
                if (j == 4) pictureBox.Top = 748;
                if (j == 5) pictureBox.Top = 788;
                if (j == 6) pictureBox.Top = 828;
                if (j == 7) pictureBox.Top = 868;
                if (j == 8) pictureBox.Top = 908;
                if (j == 9) pictureBox.Top = 948;
            }
            else if (i == 1) //東
            {
                pictureBox.Top = 507;
                if (j == 0) pictureBox.Left = 583;
                if (j == 1) pictureBox.Left = 628;
                if (j == 2) pictureBox.Left = 668;
                if (j == 3) pictureBox.Left = 708;
                if (j == 4) pictureBox.Left = 748;
                if (j == 5) pictureBox.Left = 788;
                if (j == 6) pictureBox.Left = 828;
                if (j == 7) pictureBox.Left = 868;
                if (j == 8) pictureBox.Left = 908;
                if (j == 9) pictureBox.Left = 948;
            }
            else if (i == 3) //西
            {
                pictureBox.Top = 475;
                if (j == 0) pictureBox.Left = 377;
                if (j == 1) pictureBox.Left = 337;
                if (j == 2) pictureBox.Left = 297;
                if (j == 3) pictureBox.Left = 257;
                if (j == 4) pictureBox.Left = 217;
                if (j == 5) pictureBox.Left = 177;
                if (j == 6) pictureBox.Left = 137;
                if (j == 7) pictureBox.Left = 97;
                if (j == 8) pictureBox.Left = 57;
                if (j == 9) pictureBox.Left = 17;
            }

        }

        //信号を縦を青にするメソッド，falseにすると横を青にする
        private void lightVer(bool a)
        {
            if (a == true)
            {
                lightBlue1.Visible = true; lightRed1.Visible = false;
                lightBlue2.Visible = false; lightRed2.Visible = true;
                lightBlue3.Visible = true; lightRed3.Visible = false;
                lightBlue4.Visible = false; lightRed4.Visible = true;
            }
            else
            {
                lightBlue2.Visible = true; lightRed2.Visible = false;
                lightBlue1.Visible = false; lightRed1.Visible = true;
                lightBlue4.Visible = true; lightRed4.Visible = false;
                lightBlue3.Visible = false; lightRed3.Visible = true;
            }

        }

        //pictureBoxの車に直進の動作をさせる（[i,0]のとき，つまり１番最初の車のみ対応）
        private async void straight(PictureBox pictureBox)
        {
            int i = indexI(pictureBox);
            int j = indexJ(pictureBox);

            if (i == 0) //北の車
            {
                carState[i, j] = 0;//下に進む
                control(pictureBox, true);
                await Task.Delay(900 / s);
                stop[i, 0] = false;
                pack(0);
                await Task.Delay(300 / s);
            }
            else if(i == 1) //東の車
            {
                carState[i, j] = 2;//左に進む
                control(pictureBox, true);
                await Task.Delay(900 / s);
                stop[i, 0] = false;
                pack(1);
                await Task.Delay(300 / s);

            }
            else if(i == 2) //南の車
            {
                carState[i, j] = 3;//上に進む
                control(pictureBox, true);
                await Task.Delay(900 / s);
                stop[i, 0] = false;
                pack(2);
                await Task.Delay(300 / s);
            }
            else if (i == 3) //西の車
            {
                carState[i, j] = 1;//右に進む
                control(pictureBox, true);
                await Task.Delay(900 / s);
                stop[i, 0] = false;
                pack(3);
                await Task.Delay(300 / s);
            }
            use[i, j] = false;
            pictureBox.Visible = false;

        }

        //pictureBoxの車に右折の動作をさせる（[i,0]のとき，つまり１番最初の車のみ対応）
        private async void rightTrun(PictureBox pictureBox)
        {
            int i = indexI(pictureBox);
            int j = indexJ(pictureBox);
            

            if (i == 0)
            {
                carState[i, j] = 0;
                control(pictureBox, true);
                await Task.Delay(300 / s);
                stop[i, 0] = false;
                pictureBox.Top = 510;
                pack(0);
                carState[i, j] = 2;
                await Task.Delay(1700 / s);
                control(pictureBox, false);
                
            }else if(i == 1)
            {
                carState[i, j] = 2;
                control(pictureBox, true);
                await Task.Delay(300 / s);
                stop[i, 0] = false;
                pictureBox.Left = 473;
                pack(1);
                carState[i, j] = 3;
                await Task.Delay(1800 / s);
                control(pictureBox, false);
            }
            else if (i == 2)
            {
                carState[i, j] = 3;
                control(pictureBox, true);
                await Task.Delay(300 / s);
                stop[i, 0] = false;
                pictureBox.Top = 475;
                pack(2);
                carState[i, j] = 1;
                await Task.Delay(1800 / s);
                control(pictureBox, false);
            }
            else if (i == 3)
            {
                carState[i, j] = 1;
                control(pictureBox, true);
                await Task.Delay(300 / s);
                stop[i, 0] = false;
                pictureBox.Left = 500;
                pack(3);
                carState[i, j] = 0;
                await Task.Delay(1800 / s);
                control(pictureBox, false);
            }
            use[i, j] = false;
            pictureBox.Visible = false;

        }

        //pictureBoxの車に左折の動作をさせる（[i,0]のとき，つまり１番最初の車のみ対応）
        private async void leftTrun(PictureBox pictureBox)
        {
            int i = indexI(pictureBox);
            int j = indexJ(pictureBox);

            if (i == 0)
            {
                carState[i, j] = 0;
                control(pictureBox, true);
                await Task.Delay(300 / s);
                stop[i, 0] = false;
                pictureBox.Top = 475;
                pack(0);
                carState[i, j] = 1;
                await Task.Delay(1800 / s);
                control(pictureBox, false);
            }
            else if (i == 1)
            {
                carState[i, j] = 2;
                control(pictureBox, true);
                await Task.Delay(300 / s);
                stop[i, 0] = false;
                pictureBox.Left = 505;
                pack(1);
                carState[i, j] = 0;
                await Task.Delay(1800 / s);
                control(pictureBox, false);
            }
            else if (i == 2)
            {
                carState[i, j] = 3;
                control(pictureBox, true);
                await Task.Delay(300 / s);
                stop[i, 0] = false;
                pictureBox.Top = 510;
                pack(2);
                carState[i, j] = 2;
                await Task.Delay(1800 / s);
                control(pictureBox, false);
            }
            else if (i == 3)
            {
                carState[i, j] = 1;
                control(pictureBox, true);
                await Task.Delay(300 / s);
                stop[i, 0] = false;
                pictureBox.Left = 475;
                pack(3);
                carState[i, j] = 3;
                await Task.Delay(1800 / s);
                control(pictureBox, false);
            }
            use[i, j] = false;
            pictureBox.Visible = false;

        }

        //aの列の車を詰める 0:北，1:東，2:南，3:西
        private void pack(int a)
        {
            if(stop[a, 1] == true)
            {
                for (int i = 1; i < 10; i++)
                {
                    if (stopCars[a, i] != null)
                    {
                        stopCars[a, i - 1] = stopCars[a, i];
                        stopCars[a, i] = null;
                    }
                    else
                    {
                        stop[a, i - 1] = false;
                        break;
                    }

                }
                stop[a, 0] = true;
                set();
            }
        }

        //車の動きの表示，向きの設定
        private void move(PictureBox pictureBox, int i, int j)
        {
            int spead = 50 * s;

            Bitmap bmp = (Bitmap)pictureBox.Image;
            if (direction[i, j] == 0 || direction[i, j] == 180) pictureBox.Size = new Size(25, 40);
            if (direction[i, j] == 90 || direction[i, j] == 270) pictureBox.Size = new Size(40, 25);
            if (carState[i, j] == 0) //南
            {
                if (direction[i, j] == 90) bmp.RotateFlip(RotateFlipType.Rotate90FlipNone);
                if (direction[i, j] == 0) bmp.RotateFlip(RotateFlipType.Rotate180FlipNone);
                if (direction[i, j] == 270) bmp.RotateFlip(RotateFlipType.Rotate270FlipNone);
                pictureBox.Refresh();
                pictureBox.Top += spead;
                direction[i, j] = 180;
            }
            else if (carState[i, j] == 1) //東
            {
                if (direction[i, j] == 180) bmp.RotateFlip(RotateFlipType.Rotate270FlipNone);
                if (direction[i, j] == 0) bmp.RotateFlip(RotateFlipType.Rotate90FlipNone);
                if (direction[i, j] == 270) bmp.RotateFlip(RotateFlipType.Rotate180FlipNone);
                pictureBox.Refresh();
                pictureBox.Left += spead;
                direction[i, j] = 90;
            }
            else if (carState[i, j] == 2) //西
            {
                if (direction[i, j] == 90) bmp.RotateFlip(RotateFlipType.Rotate180FlipNone);
                if (direction[i, j] == 0) bmp.RotateFlip(RotateFlipType.Rotate270FlipNone);
                if (direction[i, j] == 180) bmp.RotateFlip(RotateFlipType.Rotate90FlipNone);
                pictureBox.Refresh();
                pictureBox.Left -= spead;
                direction[i, j] = 270;
            }
            else if (carState[i, j] == 3) //北
            {
                if (direction[i, j] == 90) bmp.RotateFlip(RotateFlipType.Rotate270FlipNone);
                if (direction[i, j] == 270) bmp.RotateFlip(RotateFlipType.Rotate90FlipNone);
                if (direction[i, j] == 180) bmp.RotateFlip(RotateFlipType.Rotate180FlipNone);
                pictureBox.Refresh();
                pictureBox.Top -= spead;
                direction[i, j] = 0;
            }
        }

        //以下，各車を動かすためのメソッド//////////////////////////////////////////////////////////
        private void northCar1_go_Tick(object sender, EventArgs e)
        {
            move(northCar1, 0, 0);
 
        }        

        private void southCar1_timer_Tick(object sender, EventArgs e)
        {
            move(southCar1, 2, 0);
        }

        private void northCar2_timer_Tick(object sender, EventArgs e)
        {
            move(northCar2, 0, 1);
        }

        private void northCar3_timer_Tick(object sender, EventArgs e)
        {
            move(northCar3, 0, 2);
        }

        private void northCar4_timer_Tick(object sender, EventArgs e)
        {
            move(northCar4, 0, 3);
        }

        private void northCar5_timer_Tick(object sender, EventArgs e)
        {
            move(northCar5, 0, 4);
        }

        private void northCar6_timer_Tick(object sender, EventArgs e)
        {
            move(northCar6, 0, 5);
        }

        private void northCar7_timer_Tick(object sender, EventArgs e)
        {
            move(northCar7, 0, 6);
        }

        private void northCar9_timer_Tick(object sender, EventArgs e)
        {
            move(northCar9, 0, 8);
        }

        private void northCar8_timer_Tick(object sender, EventArgs e)
        {
            move(northCar8, 0, 7);
        }

        private void northCar10_timer_Tick(object sender, EventArgs e)
        {
            move(northCar10, 0, 9);
        }

        private void northCar11_timer_Tick(object sender, EventArgs e)
        {
            move(northCar11, 0, 10);
        }

        private void northCar12_timer_Tick(object sender, EventArgs e)
        {
            move(northCar12, 0, 11);
        }

        private void eastCar1_timer_Tick(object sender, EventArgs e)
        {
            move(eastCar1, 1, 0);
        }

        private void eastCar2_timer_Tick(object sender, EventArgs e)
        {
            move(eastCar2, 1, 1);
        }

        private void eastCar3_timer_Tick(object sender, EventArgs e)
        {
            move(eastCar3, 1, 2);
        }

        private void eastCar4_timer_Tick(object sender, EventArgs e)
        {
            move(eastCar4, 1, 3);
        }

        private void eastCar5_timer_Tick(object sender, EventArgs e)
        {
            move(eastCar5, 1, 4);
        }

        private void eastCar6_timer_Tick(object sender, EventArgs e)
        {
            move(eastCar6, 1, 5);
        }

        private void eastCar7_timer_Tick(object sender, EventArgs e)
        {
            move(eastCar7, 1, 6);
        }

        private void eastCar8_timer_Tick(object sender, EventArgs e)
        {
            move(eastCar8, 1, 7);
        }

        private void eastCar9_timer_Tick(object sender, EventArgs e)
        {
            move(eastCar9, 1, 8);
        }

        private void eastCar10_timer_Tick(object sender, EventArgs e)
        {
            move(eastCar10, 1, 9);
        }

        private void eastCar11_timer_Tick(object sender, EventArgs e)
        {
            move(eastCar11, 1, 10);
        }

        private void eastCar12_timer_Tick(object sender, EventArgs e)
        {
            move(eastCar12, 1, 11);
        }

        private void southCar2_timer_Tick(object sender, EventArgs e)
        {
            move(southCar2, 2, 1);
        }

        private void southCar3_timer_Tick(object sender, EventArgs e)
        {
            move(southCar3, 2, 2);
        }

        private void southCar4_timer_Tick(object sender, EventArgs e)
        {
            move(southCar4, 2, 3);
        }

        private void southCar5_timer_Tick(object sender, EventArgs e)
        {
            move(southCar5, 2, 4);
        }

        private void southCar6_timer_Tick(object sender, EventArgs e)
        {
            move(southCar6, 2, 5);
        }

        private void southCar7_timer_Tick(object sender, EventArgs e)
        {
            move(southCar7, 2, 6);
        }

        private void southCar8_timer_Tick(object sender, EventArgs e)
        {
            move(southCar8, 2, 7);
        }

        private void southCar9_timer_Tick(object sender, EventArgs e)
        {
            move(southCar9, 2, 8);
        }

        private void southCar10_timer_Tick(object sender, EventArgs e)
        {
            move(southCar10, 2, 9);
        }

        private void southCar11_timer_Tick(object sender, EventArgs e)
        {
            move(southCar11, 2, 10);
        }

        private void southCar12_timer_Tick(object sender, EventArgs e)
        {
            move(southCar12, 2, 11);
        }

        private void westCar1_timer_Tick(object sender, EventArgs e)
        {
            move(westCar1, 3, 0);
        }

        private void westCar2_timer_Tick(object sender, EventArgs e)
        {
            move(westCar2, 3, 1);
        }

        private void westCar3_timer_Tick(object sender, EventArgs e)
        {
            move(westCar3, 3, 2);
        }

        private void westCar4_timer_Tick(object sender, EventArgs e)
        {
            move(westCar4, 3, 3);
        }

        private void westCar5_timer_Tick(object sender, EventArgs e)
        {
            move(westCar5, 3, 4);
        }

        private void westCar6_timer_Tick(object sender, EventArgs e)
        {
            move(westCar6, 3, 5);
        }

        private void westCar7_timer_Tick(object sender, EventArgs e)
        {
            move(westCar7, 3, 6);
        }

        private void westCar8_timer_Tick(object sender, EventArgs e)
        {
            move(westCar8, 3, 7);
        }

        private void westCar9_timer_Tick(object sender, EventArgs e)
        {
            move(westCar9, 3, 8);
        }

        private void westCar10_timer_Tick(object sender, EventArgs e)
        {
            move(westCar10, 3, 9);
        }

        private void westCar11_timer_Tick(object sender, EventArgs e)
        {
            move(westCar11, 3, 10);
        }

        private void westCar12_timer_Tick(object sender, EventArgs e)
        {
            move(westCar12, 3, 11);
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }


        ////////便利メソッド////////////////////////////////////////////////////////////////////

        //pictureBoxの車のiの添え字（northCar2なら0）を返す
        private int indexI(PictureBox pictureBox)
        {
            if (pictureBox == northCar1 || pictureBox == northCar2 || pictureBox == northCar3 || pictureBox == northCar4 || pictureBox == northCar5 || pictureBox == northCar6 || pictureBox == northCar7 || pictureBox == northCar8 || pictureBox == northCar9 || pictureBox == northCar10 || pictureBox == northCar11 || pictureBox == northCar12)
            {
                return 0;
            }

            if (pictureBox == eastCar1 || pictureBox == eastCar2 || pictureBox == eastCar3 || pictureBox == eastCar4 || pictureBox == eastCar5 || pictureBox == eastCar6 || pictureBox == eastCar7 || pictureBox == eastCar8 || pictureBox == eastCar9 || pictureBox == northCar10 || pictureBox == eastCar11 || pictureBox == eastCar12)
            {
                return 1;
            }

            if (pictureBox == southCar1 || pictureBox == southCar2 || pictureBox == southCar3 || pictureBox == southCar4 || pictureBox == southCar5 || pictureBox == southCar6 || pictureBox == southCar7 || pictureBox == southCar8 || pictureBox == southCar9 || pictureBox == southCar10 || pictureBox == southCar11 || pictureBox == southCar12)
            {
                return 2;
            }

            if (pictureBox == westCar1 || pictureBox == westCar2 || pictureBox == westCar3 || pictureBox == westCar4 || pictureBox == westCar5 || pictureBox == westCar6 || pictureBox == westCar7 || pictureBox == westCar8 || pictureBox == westCar9 || pictureBox == westCar10 || pictureBox == westCar11 || pictureBox == westCar12)
            {
                return 3;
            }
            return 0;
        }

        //pictureBoxの車のjの添え字（northCar2なら1）を返す
        private int indexJ(PictureBox pictureBox)
        {
            if (pictureBox == northCar1 || pictureBox == eastCar1 || pictureBox == southCar1 || pictureBox == westCar1)
            {
                return 0;
            }
            if (pictureBox == northCar2 || pictureBox == eastCar2 || pictureBox == southCar2 || pictureBox == westCar2)
            {
                return 1;
            }
            if (pictureBox == northCar3 || pictureBox == eastCar3 || pictureBox == southCar3 || pictureBox == westCar3)
            {
                return 2;
            }
            if (pictureBox == northCar4 || pictureBox == eastCar4 || pictureBox == southCar4 || pictureBox == westCar4)
            {
                return 3;
            }
            if (pictureBox == northCar5 || pictureBox == eastCar5 || pictureBox == southCar5 || pictureBox == westCar5)
            {
                return 4;
            }
            if (pictureBox == northCar6 || pictureBox == eastCar6 || pictureBox == southCar6 || pictureBox == westCar6)
            {
                return 5;
            }
            if (pictureBox == northCar7 || pictureBox == eastCar7 || pictureBox == southCar7 || pictureBox == westCar7)
            {
                return 6;
            }
            if (pictureBox == northCar8 || pictureBox == eastCar8 || pictureBox == southCar8 || pictureBox == westCar8)
            {
                return 7;
            }
            if (pictureBox == northCar9 || pictureBox == eastCar9 || pictureBox == southCar9 || pictureBox == westCar9)
            {
                return 8;
            }
            if (pictureBox == northCar10 || pictureBox == eastCar10 || pictureBox == southCar10 || pictureBox == westCar10)
            {
                return 9;
            }
            if (pictureBox == northCar11 || pictureBox == eastCar11 || pictureBox == southCar11 || pictureBox == westCar11)
            {
                return 10;
            }
            if (pictureBox == northCar12 || pictureBox == eastCar12 || pictureBox == southCar12 || pictureBox == westCar12)
            {
                return 11;
            }
            return 0;
        }

        //車の名前を表示
        private void getName(PictureBox pictureBox)
        {
            if (pictureBox == northCar1) Debug.WriteLine("northCar1");
            if (pictureBox == northCar2) Debug.WriteLine("northCar2");
            if (pictureBox == northCar3) Debug.WriteLine("northCar3");
            if (pictureBox == northCar4) Debug.WriteLine("northCar4");
            if (pictureBox == northCar5) Debug.WriteLine("northCar5");
            if (pictureBox == northCar6) Debug.WriteLine("northCar6");
            if (pictureBox == northCar7) Debug.WriteLine("northCar7");
            if (pictureBox == northCar8) Debug.WriteLine("northCar8");
            if (pictureBox == northCar9) Debug.WriteLine("northCar9");
            if (pictureBox == northCar10) Debug.WriteLine("northCar10");
            if (pictureBox == northCar11) Debug.WriteLine("northCar11");
            if (pictureBox == northCar12) Debug.WriteLine("northCar12");
            if (pictureBox == eastCar1) Debug.WriteLine("eastCar1");
            if (pictureBox == eastCar2) Debug.WriteLine("eastCar2");
            if (pictureBox == eastCar3) Debug.WriteLine("eastCar3");
            if (pictureBox == eastCar4) Debug.WriteLine("eastCar4");
            if (pictureBox == eastCar5) Debug.WriteLine("eastCar5");
            if (pictureBox == eastCar6) Debug.WriteLine("eastCar6");
            if (pictureBox == eastCar7) Debug.WriteLine("eastCar7");
            if (pictureBox == eastCar8) Debug.WriteLine("eastCar8");
            if (pictureBox == eastCar9) Debug.WriteLine("eastCar9");
            if (pictureBox == eastCar10) Debug.WriteLine("eastCar10");
            if (pictureBox == eastCar11) Debug.WriteLine("eastCar11");
            if (pictureBox == eastCar12) Debug.WriteLine("eastCar12");
            if (pictureBox == southCar1) Debug.WriteLine("southCar1");
            if (pictureBox == southCar2) Debug.WriteLine("southCar2");
            if (pictureBox == southCar3) Debug.WriteLine("southCar3");
            if (pictureBox == southCar4) Debug.WriteLine("southCar4");
            if (pictureBox == southCar5) Debug.WriteLine("southCar5");
            if (pictureBox == southCar6) Debug.WriteLine("southCar6");
            if (pictureBox == southCar7) Debug.WriteLine("southCar7");
            if (pictureBox == southCar8) Debug.WriteLine("southCar8");
            if (pictureBox == southCar9) Debug.WriteLine("southCar9");
            if (pictureBox == southCar10) Debug.WriteLine("southCar10");
            if (pictureBox == southCar11) Debug.WriteLine("southCar11");
            if (pictureBox == southCar12) Debug.WriteLine("southCar12");
            if (pictureBox == westCar1) Debug.WriteLine("westCar1");
            if (pictureBox == westCar2) Debug.WriteLine("westCar2");
            if (pictureBox == westCar3) Debug.WriteLine("westCar3");
            if (pictureBox == westCar4) Debug.WriteLine("westCar4");
            if (pictureBox == westCar5) Debug.WriteLine("westCar5");
            if (pictureBox == westCar6) Debug.WriteLine("westCar6");
            if (pictureBox == westCar7) Debug.WriteLine("westCar7");
            if (pictureBox == westCar8) Debug.WriteLine("westCar8");
            if (pictureBox == westCar9) Debug.WriteLine("westCar9");
            if (pictureBox == westCar10) Debug.WriteLine("westCar10");
            if (pictureBox == westCar11) Debug.WriteLine("westCar11");
            if (pictureBox == westCar12) Debug.WriteLine("westCar12");
        }


        private void pictureBox1_Click(object sender, EventArgs e) 
        {
        }


        private void pictureBox1_Click_1(object sender, EventArgs e)
        {

        }

        private async void redTest_Click(object sender, EventArgs e)
        {
            northCar1.Left = 470;
            northCar1.Top = 671;
        }

        private async void blueTest_Click(object sender, EventArgs e)
        {
            eastCar1.Left = 583;
            eastCar1.Top = 507;
        }

        private async void greenTest_Click(object sender, EventArgs e)
        {
            southCar1.Left = 455;
            southCar1.Top = 400;
            carState[2, 0] = 1;
        }

        private async void purpleTest_Click(object sender, EventArgs e)
        {

        }

        private void oneEpisodeButton_Click(object sender, EventArgs e)
        {
            oneEpisode();
        }


        //テスト///////////////////////////////////////////////////////////////////////////
        


        private void label14_Click(object sender, EventArgs e)
        {

        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void label59_Click(object sender, EventArgs e)
        {

        }
    }
}