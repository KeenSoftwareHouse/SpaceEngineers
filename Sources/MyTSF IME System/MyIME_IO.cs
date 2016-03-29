using System;
using System.Text;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Forms;
using Microsoft.International.Converters;


namespace MyIME
{
	namespace Utility
	{
		/// <summary>
		/// IMEに関するクラスを静的化するための静的クラスです.
		/// </summary>
		public static partial class Statics
		{
			/// <summary>
			/// MyIMEBaseクラスの静的な機能を提供します
			/// </summary>
			public static MyIMEBase Base;
			/// <summary>
			/// MyIOクラスの静的な機能を提供します
			/// </summary>
			public static MyIO IO;
		}

		/// <summary>
		/// フォームによるキーボードからの入力から日本語文字列を生成します.
		/// </summary>
		public class MyIO : MyIMEBase
		{
			/// <summary>
			/// カーソルの表現を表す変換関数に対するデリゲートです.
			/// </summary>
			/// <param name="input">変換対象</param>
			/// <param name="index">カーソルの位置</param>
			/// <returns>変換結果</returns>
			public delegate string Fin(string input,int index);

			/// <summary>
			/// コンストラクタです.
			/// </summary>
			/// <param name="hwnd">関連付けられたハンドル</param>
			public MyIO(IntPtr hwnd)
				:this(hwnd,DefFin)
			{}
			/// <summary>
			/// コンストラクタです.
			/// </summary>
			/// <param name="hwnd">関連付けられたハンドル</param>
			/// <param name="final">カーソルの表現</param>
			public MyIO(IntPtr hwnd,Fin final)
				:base(hwnd)
			{
				useFin = final;
				Clear();
				caFlg = false;
			}
			~MyIO()
			{}

			/// <summary>
			/// フォームによるキーボードからの入力から日本語文字列を生成します.
			/// 制御キーのみ受け取ります.(制御キー以外を受け取る場合はPressCharを用いてください.
			/// </summary>
			/// <param name="input">受け取った入力</param>
			/// <returns>生成された文字列</returns>
			public string PressCtrl(ref KeyEventArgs input)
			{
				switch (input.KeyCode)
				{
					case Keys.Enter:
						{
							Flush();
							input.Handled = true;
						}
						break;
					case Keys.Space:
						{
							if (!setFlg)
							{
								this.SetTarget(t_text.ToString());
								this.SetConvert();
								setFlg = true;
							}
							int rlen = t_text.Length - l_index;
							c_String(this.Convert());
							l_index = t_text.Length - rlen;
							c_index = l_index + t_index;
							input.Handled = true;
						}
						break;
					case Keys.Right:
						{
							if (setFlg)
								this.MoveConvertTarget(1);
							else if(c_index < t_text.Length + r_text.Length)
							{
								c_index++;
								l_index = c_index - t_index;
							}
							input.Handled = true;
						}
						break;
					case Keys.Left:
						{
							if (setFlg)
								this.MoveConvertTarget(-1);
							else if(0 < c_index)
							{
								c_index--;
								l_index = c_index - t_index;
							}
							input.Handled = true;
						}
						break;
					case Keys.Up:
						{
							int rlen = t_text.Length - l_index;
							c_String(this.ConvertUp());
							input.Handled = true;
							l_index = t_text.Length - rlen;
							c_index = l_index + t_index;
						}
						break;
					case Keys.Down:
						{
							int rlen = t_text.Length - l_index;
							c_String(this.ConvertDown());
							input.Handled = true;
							l_index = t_text.Length - rlen;
							c_index = l_index + t_index;
						}
						break;
					case Keys.Back:
						{
							if (c_index > 0)
							{
								if (t_index < c_index && c_index <= t_index + t_text.Length)
								{
									int rlen = t_text.Length - l_index;
									t_text.Remove(l_index - 1, 1);
									l_index = t_text.Length - rlen;
									c_index = l_index + t_index;
								}
								else if (t_index >= c_index)
								{
									r_text.Remove(c_index - 1, 1);
									t_index--;
									c_index = l_index + t_index;
								}
								else
								{
									r_text.Remove(c_index - t_text.Length - 1, 1);
									c_index--;
								}
							}
							input.Handled = true;
						}
						break;
					case Keys.Delete:
						{
							if (c_index < t_text.Length + r_text.Length)
							{
								if (t_index <= c_index && c_index < t_index + t_text.Length)
								{
									int rlen = t_text.Length - l_index;
									t_text.Remove(l_index, 1);
									l_index = t_text.Length - rlen;
									c_index = l_index + t_index;
								}
								else if (t_index > c_index)
								{
									r_text.Remove(c_index, 1);
									t_index--;
								}
								else
								{
									r_text.Remove(c_index - t_text.Length, 1);
								}
							}
							input.Handled = true;
						}
						break;
				}
				eaFlg = input.Handled;
				return e_Press();
			}
			/// <summary>
			/// フォームによるキーボードからの入力から日本語文字列を生成します.
			/// 制御キー以外を受け取ります.(制御キーを受け取る場合はPressCtrlを用いてください.
			/// </summary>
			/// <param name="input">受け取った入力</param>
			/// <returns>生成された文字列</returns>
			public string PressChar(KeyPressEventArgs input)
			{
				if (!eaFlg)
				{
					if (insFlg && 0 <= l_index && l_index <= t_text.Length)
						return m_Press(input.KeyChar);
					else if (!insFlg)
					{
						t_index = c_index;
						l_index = 0;
						insFlg = true;
						return m_Press(input.KeyChar);
					}
				}
				return e_Press();
			}
			/// <summary>
			/// 初期化します.
			/// </summary>
			/// <param name="m_text">対象文字列</param>
			/// <param name="r_index">カーソルの位置</param>
			public void Clear(string m_text = "",int r_index = 0)
			{
				setFlg = false;
				nFlg = false;
				c_index = r_index;
				l_index = 0;
				t_index = r_index;
				t_text = new StringBuilder();
				r_text = new StringBuilder(m_text);
			}
			/// <summary>
			/// 変換するか否か取得、設定します.
			/// </summary>
			public bool ConvertAble
			{
				get { return caFlg; }
				set
				{
					if (!value)
						Flush();
					caFlg = value;
				}
			}

			string m_Press(char input)
			{
				if (caFlg)
				{
					switch (input)
					{
						case 'n':
						case 'N':
							{
								int rlen = t_text.Length - l_index;
								t_text.Insert(l_index, input);
								if (nFlg)
									c_String(KanaConverter.RomajiToHiragana(t_text.ToString()));
								nFlg = !nFlg;
								l_index = t_text.Length - rlen;
								c_index = l_index + t_index;
							}
							break;
						default:
							{
								int rlen = t_text.Length - l_index;
								t_text.Insert(l_index, input);
								c_String(KanaConverter.RomajiToHiragana(t_text.ToString()));
								nFlg = false;
								l_index = t_text.Length - rlen;
								c_index = l_index + t_index;
							}
							break;
					}
				}
				else
				{
					int rlen = r_text.Length - t_index;
					r_text.Insert(c_index, input);
					t_index = r_text.Length - rlen;
					c_index = l_index + t_index;
				}
				return e_Press();
			}

			void c_String(string t)
			{
				int rlen = t_text.Length - l_index;
				t_text = new StringBuilder(t);
				l_index = t_text.Length - rlen;
				c_index = l_index;
			}
			string e_Press()
			{
				return useFin(s_Press(),c_index);
			}
			string s_Press()
			{
				StringBuilder ans = new StringBuilder(r_text.ToString());
				return ans.Insert(t_index,t_text).ToString();
			}
			void Flush()
			{
				int rlen = t_text.Length + r_text.Length - c_index;
				setFlg = false;
				insFlg = false;
				r_text = new StringBuilder(s_Press());
				t_text = new StringBuilder();
				c_index = r_text.Length - rlen;
				t_index = c_index;
				l_index = 0;
			}

			/// <summary>
			/// 標準の変換関数です.
			/// </summary>
			public static string DefFin(string input,int index)
			{
				return input.Insert(index, "|");
			}

			int l_index;//文字の挿入位置
			int c_index;//カーソルの位置
			int t_index;//挿入開始位置
			StringBuilder t_text;
			StringBuilder r_text;
			bool setFlg, nFlg, insFlg, eaFlg;
			bool caFlg;
			Fin useFin;
		}
	}
}
