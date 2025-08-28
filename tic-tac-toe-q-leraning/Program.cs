using System;
using System.IO.Pipelines;
using System.Net;
using System.Runtime.CompilerServices;
using System.IO;
using System.Collections.Generic;
using OfficeOpenXml;

//
// マルバツゲーム（TicTacToe）のプログラム
//
namespace TicTacToeAI
{
    // マルバツゲームの盤面を管理するクラス(ここは全員共通のクラス)
    public class Game
    {
        public int[] Board { get; private set; } = new int[9];
        // 盤面の状態を保持（0：未選択，1:〇1, 2:〇2, 3:〇3, 4:×1, 5:×2, 6:×3）
        public bool IsFinished { get; private set; } = false; // ゲーム終了フラグ
        public int Winner { get; private set; } = 0; // 勝者のプレイヤー番号（0：引き分け、1：先攻、2：後攻）
        // 0は勝者がいない状態も示す
        public int turn = 1; // 現在のターン数 

        // マスの状態をリセット
        public void Reset()
        {
            Board = new int[9];
            IsFinished = false;
            Winner = 0;
        }

        /// マスの状態を更新 
        public bool MakeMove(int position, int player)
        {
            // 6ターン以内なら置くだけ
            if (turn <= 6 && Board[position] == 0)
            {
                if (player == 1)
                {
                    // 1か2ターン目なら1，3か4ターン目なら2を置く
                    if (turn == 1 || turn == 2)
                    {
                        Board[position] = 1;
                    }
                    else if (turn == 3 || turn == 4)
                    {
                        Board[position] = 2;
                    }
                    else if (turn == 5 || turn == 6)
                    {
                        Board[position] = 3;
                    }
                    CheckForWinner();
                    return true;
                }
                else if (player == 2)
                {
                    // 1か2ターン目なら4，3か4ターン目なら5を置く
                    if (turn == 1 || turn == 2)
                    {
                        Board[position] = 4;
                    }
                    else if (turn == 3 || turn == 4)
                    {
                        Board[position] = 5;
                    }
                    else if (turn == 5 || turn == 6)
                    {
                        Board[position] = 6;
                    }
                    CheckForWinner();
                    return true;
                }
            }
            // 7ターン目以上なら，置いて古いやつ消す
            else if (turn >= 7 && Board[position] == 0)
            {
                if (player == 1)
                {
                    // 盤面上の〇の箇所の状態を全て-1
                    for (int i = 0; i < 9; i++)
                    {
                        if (1 <= Board[i] && Board[i] <= 3)
                        {
                            Board[i] -= 1;
                        }
                    }
                    Board[position] = 3;
                    CheckForWinner();
                    return true;
                }
                else if (player == 2)
                {
                    for (int i = 0; i < 9; i++)
                    {
                        // 盤面上の×の箇所で，4なら0に，5か6なら未選択にする
                        if (4 == Board[i])
                        {
                            Board[i] = 0;
                        }
                        else if (5 == Board[i] || Board[i] == 6)
                        {
                            Board[i] -= 1;
                        }
                    }
                    Board[position] = 6;
                    CheckForWinner();
                    return true;
                }
            }
            return false;
        }

        // 勝利判定
        private void CheckForWinner()
        {
            // タテ、ヨコ、ナナメのチェック
            int[][] winConditions = {
                [0, 1, 2], [3, 4, 5], [6, 7, 8], // 横
                [0, 3, 6], [1, 4, 7], [2, 5, 8], // 縦
                [0, 4, 8], [2, 4, 6]  // 斜め
            };

            // 勝敗判定できるように，〇か×かの状態に変換
            int[] nowBoard = new int[9];
            for (int i = 0; i < 9; i++)
            {
                if (Board[i] == 0 || Board[i] == 1 || Board[i] == 4)
                {
                    nowBoard[i] = Board[i];
                }
                else if (Board[i] == 2 || Board[i] == 3)
                {
                    nowBoard[i] = 1;
                }
                else if (Board[i] == 5 || Board[i] == 6)
                {
                    nowBoard[i] = 4;
                }
            }

            // 全てのパターンを確認し，勝敗が決まっているかチェック
            foreach (var condition in winConditions)
            {
                if (nowBoard[condition[0]] != 0 &&
                    nowBoard[condition[0]] == nowBoard[condition[1]] &&
                    nowBoard[condition[1]] == nowBoard[condition[2]])
                {
                    Winner = nowBoard[condition[0]]; // そろっている記号（1か2）をWinnerとする．
                    // if (Winner == 1) Console.Write("勝者は，agentです！\n");
                    // else Console.Write("勝者は，opponentです！\n");
                    IsFinished = true;
                }
            }

            // // すべてのマスが埋まっているかどうかを確認（引き分けの場合）
            // if (Array.IndexOf(Board, 0) == -1)
            // {
            //     IsFinished = true;
            // }
        }

        // リーチの判定
        public bool CheckForReach(int player)
        {
            // 勝利条件のパターン
            int[][] winConditions = {
                [0, 1, 2], [3, 4, 5], [6, 7, 8], // 横
                [0, 3, 6], [1, 4, 7], [2, 5, 8], // 縦
                [0, 4, 8], [2, 4, 6]  // 斜め
            };

            foreach (var condition in winConditions)
            {
                int count = 0;
                int emptyPosition = -1;

                for (int i = 0; i < 3; i++)
                {
                    // 6ターン目までは，最古の記号が消えない前提で判定
                    if (turn <= 6)
                    {
                        if (Board[condition[i]] == 0)
                        {
                            emptyPosition = condition[i];
                        }
                        else if (((Board[condition[i]] == 1 || Board[condition[i]] == 2) && player == 1) ||
                                ((Board[condition[i]] == 4 || Board[condition[i]] == 5) && player == 2))
                        {
                            count++;
                        }
                    }
                    else if (turn >= 7)
                    {
                        if (Board[condition[i]] == 0 || Board[condition[i]] == 1 || Board[condition[i]] == 4)
                        {
                            emptyPosition = condition[i];
                        }
                        else if (((Board[condition[i]] == 2 || Board[condition[i]] == 3) && player == 1) ||
                                ((Board[condition[i]] == 5 || Board[condition[i]] == 6) && player == 2))
                        {
                            count++;
                        }
                    }

                }

                if (count == 2 && emptyPosition != -1)
                {
                    return true; // リーチ状態
                }
            }

            return false; // リーチ状態ではない
        }

        // 報酬の獲得
        public int getReawrd()
        {
            int reward = 0;
            if (IsFinished)
            {
                if (Winner == 1)
                {
                    reward += 3;
                }
                else
                {
                    reward -= 3;
                }
            }
            if (CheckForReach(1))
            {
                reward += 1;
            }
            if (CheckForReach(2))
            {
                reward -= 1;
            }

            return reward;
        }

        // 現在の盤面と状態を表示
        public void BoardDisplay(int[] Board)
        {
            for (int i = 0; i < 9; i++)
            {
                Console.Write(" {0} ", Board[i]);
            }
            Console.WriteLine();
            for (int i = 0; i < 9; i++)
            {
                String pixel = "";
                if (Board[i] == 0) pixel = "  ";
                else if (1 <= Board[i] && Board[i] <= 3)
                {
                    pixel = "o" + Board[i].ToString(); // エージェントは〇
                }
                else
                {
                    pixel = "x" + (Board[i] - 3).ToString(); // 対戦相手は×
                }
                Console.Write(" {0} ", pixel);
                if (i % 3 != 2) Console.Write('|');
                else
                {
                    Console.WriteLine();
                    if (i != 8) Console.WriteLine("----+----+----");
                    else Console.WriteLine();
                }
            }
        }

        // 現在の盤面から状態を計算（7進数9bitを10進数に変換）
        public int CalculateState()
        {
            int state = 0;
            for (int i = 0; i < 9; i++)
            {
                state += Board[i] * (int)Math.Pow(7, i); // 3進数9bitを10進数に変換
            }
            return state;
        }

        // 10進数を7進数9bitに変換する処理
        public static string ConvertTo9Bit3Base(int decimalNumber)
        {
            // Step 1: 10進数を7進数に変換
            string base7 = Convert.ToString(decimalNumber, 7);

            // Step 2: 9ビットに調整
            base7 = base7.PadLeft(9, '0');  // 9桁未満の場合、先頭に0を追加
            if (base7.Length > 9)
            {
                base7 = base7.Substring(base7.Length - 9);  // 9桁を超える場合、下位9桁を取得
            }

            return base7;
        }
    }

    // プレイヤーの行動を制御する抽象クラス（ここも全員共通のクラス）
    public abstract class Player
    {
        public int Id { get; private set; }
        protected LearningAlgorithm LearningAlgorithm;

        protected Player(int id, LearningAlgorithm learningAlgorithm)
        {
            Id = id;
            LearningAlgorithm = learningAlgorithm;
        }


        // 行動選択のメソッド
        public abstract int ChooseAction(Game game);
    }

    public class OpponentPlayer : Player
    {
        public OpponentPlayer(int id, LearningAlgorithm learningAlgorithm) : base(id, learningAlgorithm) { }

        public override int ChooseAction(Game game)
        {
            return LearningAlgorithm.randomAction(game, Id); // 対戦相手はランダムな行動選択
        }

    }

    // 学習アルゴリズムを用いるプレイヤー
    public class LearningPlayer : Player
    {
        public LearningPlayer(int id, LearningAlgorithm learningAlgorithm) : base(id, learningAlgorithm) { }

        public override int ChooseAction(Game game)
        {
            return LearningAlgorithm.SelectAction(game, Id); // エージェントはSelectActionで行動選択をする
        }
    }

    // 学習アルゴリズムのインターフェース
    public interface LearningAlgorithm
    {
        int SelectAction(Game game, int playerId); // 行動選択
        int randomAction(Game game, int playerId); // ランダムな行動選択
        void Update(int state, int action, int reward, int nextState); // Q値などの更新（Q学習専用）
        void addQtable(int state); // Qテーブルに状態を追加
        void QtableDisplay(); // Qテーブルの一部表示（Q学習専用）
        Dictionary<int, double[]> QTable { get; } // Qテーブルを取得するためのプロパティ 
    }

    // Q学習アルゴリズムの実装
    public class QLearning : LearningAlgorithm
    {
        Dictionary<int, double[]> Q = new Dictionary<int, double[]>(); //Qテーブル
        Dictionary<int, double> maxQ = new Dictionary<int, double>(); // Qの最大
        private double epsilon, alpha, gamma;
        private Random random;

        public QLearning(double epsilon = 0.3, double alpha = 0.7, double gamma = 0.4)
        {
            this.epsilon = epsilon; // 行動選択時にランダムな箇所に置く確率
            this.alpha = alpha; // 学習率
            this.gamma = gamma; // 割引率
            random = new Random();
        }

        // Qテーブルにアクセスするためのプロパティを追加
        public Dictionary<int, double[]> QTable
        {
            get { return Q; }
        }

        // 行動選択（εグリーディ方式）
        public int SelectAction(Game game, int playerId)
        {
            int state = game.CalculateState();
            if (random.NextDouble() < epsilon) // ランダムな行動選択
            {
                return randomAction(game, playerId);
            }
            else
            {
                // その状態において，Q値が全て等しいとき，ランダムに行動
                if (Array.TrueForAll(Q[state], n => n == Q[state][0]))
                {
                    // Console.Write("Q値に基づいて行動しようとしたが，Q値が全て等しかったため，");
                    return randomAction(game, playerId);
                }
                // その状態から最もQ値の高いマスを探す
                int bestAction = 0;
                double maxValue = Q[state][0];
                for (int i = 1; i < 9; i++)
                {
                    if (Q[state][i] >= maxValue && game.Board[i] == 0) // おけるかどうかの確認も同時にする
                    {
                        bestAction = i;
                        maxValue = Q[state][i];
                    }
                }
                // if (playerId == 1) Console.WriteLine("agentがQ値に基づいて行動");
                // else Console.WriteLine("opponentがQ値に基づいて行動");
                return bestAction;
            }
        }

        // ランダムな行動選択
        public int randomAction(Game game, int playerId)
        {
            while (true)
            {
                int ran = random.Next(0, 9);
                if (game.Board[ran] == 0) // おけるかどうかの確認
                {
                    // if (playerId == 1) Console.WriteLine("agentがランダムに行動");
                    // else Console.WriteLine("opponentがランダムに行動");
                    return ran;
                }
            }
        }

        // Q値の更新（Q学習専用）
        public void Update(int state, int action, int reward, int nextState)
        {
            Q[state][action] = (1 - alpha) * Q[state][action] + alpha * (reward + gamma * maxQ[nextState]); // Q値を計算，更新
            if (Q[state][action] > maxQ[state])
            {
                maxQ[state] = Q[state][action];
            }
        }

        // Qテーブルに状態を追加(既に状態がある場合は追加しない)
        public void addQtable(int state)
        {
            double[] newArray = { 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0 };
            if (!Q.ContainsKey(state)) // 既にその状態がある場合
            {
                Q.Add(state, newArray);
                maxQ.Add(state, 0.0);
            }
            return;
        }



        // Qtableの表示（Q学習専用）
        public void QtableDisplay()
        {
            Console.WriteLine("    Q     |     0     |     1     |     2     |     3     |     4     |     5     |     6     |     7     |     8     |");
            Console.WriteLine("----------+-----------+-----------+-----------+-----------+-----------+-----------+-----------+-----------+-----------|");
            foreach (var kvp in Q)
            {
                Console.Write(String.Format(" {0,8} ", kvp.Key));
                foreach (var value in kvp.Value)
                {
                    Console.Write(string.Format("| {0,9:0.######} ", value));
                }
                Console.WriteLine(string.Format("|"));
            }
            Console.WriteLine("現在の状態数:{0}", Q.Count);
            Console.WriteLine();
        }
    }

    // プログラムの実行
    class Program
    {
        static void Main(string[] args)
        {
            Game game = new Game();
            LearningAlgorithm Learning = new QLearning(); // Q学習を指定（違う機械学習ならここを変更？）
            Player agent = new LearningPlayer(1, Learning); // エージェント
            Player opponent = new OpponentPlayer(2, Learning); // 対戦相手  

            List<int> rewardRecord = new List<int>(); // エピソードごとの累計獲得報酬
            List<int> actionRecord = new List<int>(); // エピソードごとの累計行動実行回数
            List<double> rewardEffi = new List<double>(); // エピソードごとの報酬獲得効率
            List<double> winPer = new List<double>(); // 数エピソードごとの勝率
            int winCount = 0; // 勝利回数のカウント
            int RewardCount = 0; // 獲得報酬のカウント
            int actionCount = 0; // エージェントの行動実行回数のカウント

            int episode = 1000000; // エピソードの繰り返し数
            for (int i = 1; i <= episode; i++) // エピソードの繰り返し
            {

                game.Reset(); // マスの状態をリセット
                game.turn = 1; // ターン数を1からスタート
                Player nowPlayer = agent; // 現在の手のプレイヤー

                // 50%の確率で対戦相手が先に動く（対戦相手が先攻）
                Random random = new Random();
                if (random.NextDouble() <= 0.5) nowPlayer = opponent;

                Learning.addQtable(game.CalculateState()); // 初期状態をQテーブルに追加

                while (!game.IsFinished) // ゲームが決着着くまで繰り返す
                {
                    // Learning.QtableDisplay(); //Qテーブルの表示
                    int preState = game.CalculateState(); // 1手を置く前の状態

                    // game.BoardDisplay(game.Board); // 現在のボードを表示 

                    // 現在，何ターン目か誰のターンか表示
                    // Console.WriteLine();
                    // Console.WriteLine("{0}ターン目", game.turn);
                    // if (nowPlayer == agent) Console.WriteLine("agentのターン");
                    // else Console.WriteLine("opponentのターン");

                    int action = nowPlayer.ChooseAction(game); // 現在のプレイヤーが行動選択

                    // どこに設置したか表示
                    // Console.WriteLine("{0}に設置！", action);
                    // Console.WriteLine();

                    if (game.MakeMove(action, nowPlayer.Id))
                    {
                        int nextState = game.CalculateState(); // 1手置いた後の状態 
                        Learning.addQtable(nextState); // Qテーブルに新しい状態を追加

                        // 現在，リーチかどうか表示
                        // if (!game.IsFinished)
                        // {
                        //     for (int j = 1; j <= 2; j++)
                        //     {
                        //         if (game.CheckForReach(j))
                        //         {
                        //             if (j == 1) Console.WriteLine("現在，agentのリーチ！");
                        //             else Console.WriteLine("現在，opponentのリーチ！");
                        //         }
                        //     }
                        // }

                        // agentなら報酬の獲得やQ値の更新を行う
                        if (nowPlayer == agent)
                        {
                            int reward = game.getReawrd(); // 報酬の獲得
                            Learning.Update(preState, action, reward, nextState); // Q値の更新
                            RewardCount += reward; // 報酬のカウントに追加
                            actionCount += 1; // 行動回数をインクリメント
                        }
                    }

                    // エージェントが勝った場合，勝利回数をインクリメント
                    if (game.Winner == agent.Id) winCount += 1;

                    // プレイヤー交代
                    if (nowPlayer == agent) nowPlayer = opponent;
                    else nowPlayer = agent;

                    game.turn += 1;
                }
                // game.BoardDisplay(game.Board); // 現在のボードを表示 

                // 1000ターンごとに記録
                if (i == 1 || i % 1000 == 0)
                {
                    rewardRecord.Add(RewardCount); // このエピソードでの累計獲得報酬
                    actionRecord.Add(actionCount); // このエピソードでの累計行動回数
                    rewardEffi.Add((double)RewardCount / actionCount); // このエピソードでの累計獲得報酬
                    winPer.Add(winCount / 1000.0 * 100.0); // 1000エピソードごとの勝率を計算
                    winCount = 0; // 勝利回数のリセット
                    RewardCount = 0; // 累計報酬獲得のリセット
                    actionCount = 0; // 累計行動実行回数のリセット
                }

            }
            // Learning.QtableDisplay(); // Qテーブルの表示

            // Excelへの書き込み
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Sheet1");

                // ターン数の書き込み
                worksheet.Cells[1, 1].Value = "エピソード数";
                for (int i = 0; i < rewardRecord.Count; i++)
                {
                    worksheet.Cells[i + 2, 1].Value = 1000 * i;
                }

                // 各分析の書き込み
                worksheet.Cells[1, 2].Value = "累計獲得効率";
                for (int i = 0; i < rewardRecord.Count; i++)
                {
                    worksheet.Cells[i + 2, 2].Value = rewardRecord[i];
                }

                worksheet.Cells[1, 3].Value = "累計行動実行回数";
                for (int i = 0; i < actionRecord.Count; i++)
                {
                    worksheet.Cells[i + 2, 3].Value = actionRecord[i];
                }

                worksheet.Cells[1, 4].Value = "1000エピソードごとの報酬獲得効率";
                for (int i = 0; i < rewardEffi.Count; i++)
                {
                    worksheet.Cells[i + 2, 4].Value = rewardEffi[i];
                }

                worksheet.Cells[1, 5].Value = "1000エピソードごとの勝率";
                for (int i = 0; i < winPer.Count; i++)
                {
                    worksheet.Cells[i + 2, 5].Value = winPer[i];
                }

                // Qテーブルの書き込み
                var worksheet2 = package.Workbook.Worksheets.Add("Sheet2");
                worksheet2.Cells[1, 1].Value = "状態/行動";
                for (int i = 0; i < 9; i++)
                {
                    worksheet2.Cells[1, i + 2].Value = i;
                }

                // Qテーブルのデータを取得
                Dictionary<int, double[]> qTable = Learning.QTable;

                int row = 2;
                foreach (var entry in qTable)
                {
                    worksheet2.Cells[row, 1].Value = entry.Key; // 状態
                    for (int j = 0; j < entry.Value.Length; j++)
                    {
                        worksheet2.Cells[row, j + 2].Value = entry.Value[j]; // Q値
                    }
                    row++;
                }

                FileInfo excelFile = new FileInfo("output.xlsx");
                package.SaveAs(excelFile);
            }

            // Console.WriteLine("Excelファイルが作成されました。");
        }
    }
}
