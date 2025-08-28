
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MyQLProblems;
using static System.Collections.Specialized.BitVector32;

namespace Q学習
{
    internal class Program
    {

        static double[,] qTable = new double[10, 3];
        static Random rand = new Random();
        static int currentState;

        static void Main()
        {
            
            // 最終エピソードにおける・・・
            double actionFinalCount = 0.0; // 行動実行回数
            double rewardFinalCount = 0.0; // 獲得報酬
            double[] efficient = new double[26]; // 20エピソードごとの報酬獲得効率
            for (int i = 0; i < 26; i++) efficient[i] = 0.0;
/*
           for (int k = 0; k < 100; k++) // プログラムを100回試行するやつ
           {
*/
                int count = 0;

                double actionCount = 0.0; // 累計行動実行回数
                double rewardCount = 0.0; // 累計獲得報酬

                // ハイパーパラメータ
                double learningRate = 0.5;   // 学習率α
                double discountFactor = 0.7; // 割引率γ

                // Qテーブルの初期化
                for (int i = 0; i < 10; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        qTable[i, j] = 0.0;
                    }
                }

                // Q学習のメインループ
                for (int episode = 0; episode < 500; episode++)
                {
                    //System.Diagnostics.Debug.WriteLine("--------------------------------");

                    MyQLProblems.Problem1.initState(); // 初期状態にリセット

                    while (!MyQLProblems.Problem1.getStateIsGoal())
                    {
                        int oldState = MyQLProblems.Problem1.getState(); // Q値を更新したい状態

                        int action = chooseAction(oldState); // アクションを決める
                        //int action = rand.Next(3); // 行動を完全ランダムで決める
                                                             //System.Diagnostics.Debug.WriteLine("行動:" + action );

                        double reward = MyQLProblems.Problem1.doAction(action); // 行動を実行して次の状態と報酬を取得
                                                                                //System.Diagnostics.Debug.WriteLine("状態:" + MyQLProblems.Problem1.getState() +  "  reward:" + reward);

                        currentState = MyQLProblems.Problem1.getState(); // Q値を更新したい状態の次の状態 (s')

                        // 一つ前の状態のQ値の更新
                        double maxCurrentQValue = getMaxQValue(currentState); // 現在の状態の最大のQ値
                        double newQValue = (1 - learningRate) * qTable[oldState,action] + learningRate * (reward + discountFactor * maxCurrentQValue); // Q(s,a) <- (1-a)Q(s,a)+a[r+γmaxQ(s',a')]]
                        qTable[oldState, action] = newQValue; // Q値を更新

                        rewardCount += reward; // 累計獲得報酬に追加
                        actionCount++; // 累計行動実行回数をカウント

                        if (episode == 499) // 最終エピソード時
                        {
                            actionFinalCount++;
                            rewardFinalCount += reward;
                        }
                    }
                    if (episode == 0 || (episode + 1) % 20 == 0)
                    {
                        efficient[count] += (rewardCount / actionCount);
                        System.Diagnostics.Debug.WriteLine((episode + 1) + "エピソード目");
                        System.Diagnostics.Debug.WriteLine("報酬獲得効率 : " + (rewardCount / actionCount));
                        count++;
                    }
                }

                // 学習結果の表示
                System.Diagnostics.Debug.WriteLine("Learned Q Table:");
                System.Diagnostics.Debug.WriteLine("|---------------------------|");
                System.Diagnostics.Debug.WriteLine("|      |       行動 a       |");
                System.Diagnostics.Debug.WriteLine("|---------------------------|");
                System.Diagnostics.Debug.WriteLine("|状態 s|  0   |  1   |  2   |");
                System.Diagnostics.Debug.WriteLine("|---------------------------|");
                for (int i = 0; i < 10; i++)
                {
                    System.Diagnostics.Debug.WriteLine("|   " + i + $"  | {string.Format("{0, -4}", Math.Round(qTable[i, 0], 2))} | {string.Format("{0, -4}", Math.Round(qTable[i, 1], 2))} | {string.Format("{0, -4}", Math.Round(qTable[i, 2], 2))} |");
                }
                System.Diagnostics.Debug.WriteLine("|---------------------------|");

                System.Diagnostics.Debug.WriteLine("行動実行回数の平均 : " + (actionCount / 500));
                System.Diagnostics.Debug.WriteLine("獲得報酬の平均 : " + (rewardCount / 500));
               
            /*
                }
                for (int i = 0; i < 26;i++) {
                    System.Diagnostics.Debug.WriteLine(efficient[i] / 100);
                }
                System.Diagnostics.Debug.WriteLine("最終エピソードにおける行動実行回数の平均 : " + (actionFinalCount / 100));
                System.Diagnostics.Debug.WriteLine("最終エピソードにおける獲得報酬の平均 : " + (rewardFinalCount / 100));
                System.Diagnostics.Debug.WriteLine("最終エピソードにおける報酬獲得効率の平均 : " + (rewardFinalCount / actionFinalCount));
            */
        }

        // 引数stateの最大のQ値を返す
        static double getMaxQValue(int state)
        {
            double maxQValue = qTable[state, 0];

            for (int i = 1; i < 3; i++)
            {
                if (qTable[state, i] > maxQValue)
                {
                    maxQValue = qTable[state, i];
                }
            }

            return maxQValue;
        }

        // ε-greedy法による行動の選択
        static int chooseAction(int oldState)
        {
            double epsilon = 0.3;
            if (rand.Next(0,100) < epsilon * 100)
            {
                return rand.Next(3); // 0から2の乱数を返す
               
            }
            else
            {
                return getBestAction(oldState);

            }
        }

        // Qテーブルから最適な行動を取得するメソッド
        static int getBestAction(int state)
        {
            double maxQValue = qTable[state, 0];
            int bestAction = 0;

            for (int i = 1; i < 3; i++)
            {
                if (qTable[state, i] > maxQValue)
                {
                    maxQValue = qTable[state, i];
                    bestAction = i;
                }
            }

            return bestAction;
        }
    }
}
