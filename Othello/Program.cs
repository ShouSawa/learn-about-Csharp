using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Reversi {
	static class Program {
		/// <summary>
		/// アプリケーションのメイン エントリ ポイントです。
		/// </summary>
		[STAThread]
		static void Main() {
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

			//DLL化のために変更
			BasicAI ai = new BasicAI();
			Application.Run(new MainForm(ai));
		}
	}
}
