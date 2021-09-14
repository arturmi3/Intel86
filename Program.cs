
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using System.Diagnostics;

namespace SymulatorX86
{
	public class Program
	{
		private const int ROZMIAR_PAMIĘCI = 1000;
        private const string NazwaPliku = "Program.txt";
        
		static ConsoleColor KolorInstrukcji = (Console.BackgroundColor == ConsoleColor.Black) ? ConsoleColor.Yellow : ConsoleColor.Cyan;

		public class LiniaProgramu
		{
			public string Etykieta;
			public string Instrukcja;
			public string Op1;
			public string Op2;

			public string LiniaŹródłowa;

			public LiniaProgramu(string etykieta, string instrukcja, string op1, string op2, string liniaŹródłowa)
			{
				Etykieta = etykieta;
				Instrukcja = instrukcja;
				Op1 = op1;
				Op2 = op2;
				LiniaŹródłowa = liniaŹródłowa;
			}
		}

		public class Rejestry
		{
			enum Flagi : UInt16
			{
				CF = 0x0001,
				PF = 0x0004,
				ZF = 0x0040,
				SF = 0x0080,
				//OF = 0x0800
			};

			UInt16 ax;  // accumulator
			UInt16 bx;  // base register
			UInt16 cx;  // counter register
			UInt16 dx;  // data register
			UInt16 sp;  // stack pointer            
			UInt16 ip;  // instruction pointer
			UInt16 flags; // flags register

			UInt16 ax_;  // poprzednia wartość ax
			UInt16 bx_;  // poprzednia wartość base register
			UInt16 cx_;  // poprzednia wartość counter register
			UInt16 dx_;  // poprzednia wartość data register
			UInt16 sp_;  // poprzednia wartość stack pointer            
			UInt16 ip_;  // poprzednia wartość instruction pointer
			UInt16 flags_; // poprzednia wartość flags register

			public UInt16 AX
			{
				get { return ax; }
				set { ax_ = ax; ax = value; }
			}
			public UInt16 BX
			{
				get { return bx; }
				set { bx_ = bx; bx = value; }
			}
			public UInt16 CX
			{
				get { return cx; }
				set { cx_ = cx; cx = value; }
			}
			public UInt16 DX
			{
				get { return dx; }
				set { dx_ = dx; dx = value; }
			}

			public byte AH
			{
				get { return (byte)(ax >> 8); }
				set { ax_ = ax; ax = (UInt16)((ax & 0xFF00) | value); }
			}
			public byte AL
			{
				get { return (byte)(ax & 0xFF); }
				set { ax_ = ax; ax = (UInt16)(value | (ax & 0xff)); }
			}
			public byte BH
			{
				get { return (byte)(bx >> 8); }
				set { bx_ = bx; bx = (UInt16)((bx & 0xff00) | value); }
			}
			public byte BL
			{
				get { return (byte)(bx & 0xFF); }
				set { bx_ = bx; bx = (UInt16)(value | (bx & 0xff)); }
			}
			public byte CH
			{
				get { return (byte)(cx >> 8); }
				set { cx_ = cx; cx = (UInt16)((cx & 0xff00) | value); }
			}
			public byte CL
			{
				get { return (byte)(cx & 0xFF); }
				set { cx_ = cx; cx = (UInt16)(value | (cx & 0xff)); }
			}
			public byte DH
			{
				get { return (byte)(dx >> 8); }
				set { dx_ = dx; dx = (UInt16)((dx & 0xff00) | value); }
			}
			public byte DL
			{
				get { return (byte)(dx & 0xFF); }
				set { dx_ = dx; dx = (UInt16)(value | (dx & 0xff)); }
			}

			public UInt16 SP
			{
				get { return sp; }
				set { sp_ = sp; sp = value; }
			}

			public UInt16 IP
			{
				get { return ip; }
				set { ip_ = bx; ip = value; }
			}

			public UInt16 FLAGS
			{
				get { return flags; }
				set { flags_ = flags; flags = value; }
			}

			public UInt16 UstawFlagi(bool CF, bool PF, bool ZF, bool SF)    //, bool OF)
			{
				// aktualna wartość
				UInt16 value = flags;
				if (CF) value |= (UInt16)Flagi.CF; else value &= (ushort)~(Flagi.CF);
				if (PF) value |= (UInt16)Flagi.PF; else value &= (ushort)~(Flagi.PF);
				if (ZF) value |= (UInt16)Flagi.ZF; else value &= (ushort)~(Flagi.ZF);
				if (SF) value |= (UInt16)Flagi.SF; else value &= (ushort)~(Flagi.SF);
				//if (OF) value |= (UInt16)Flagi.OF; else value &= (ushort)~(Flagi.OF);

				flags_ = flags;
				flags = value;
				return flags;
			}
			public bool Flaga_CF    // carry flag
			{
				get { return ((flags & (UInt16)Flagi.CF) == (UInt16)Flagi.CF); }
			}
			public bool Flaga_PF    // parity flag
			{
				get { return ((flags & (UInt16)Flagi.PF) == (UInt16)Flagi.PF); }
			}
			public bool Flaga_ZF    // zero flag
			{
				get { return ((flags & (UInt16)Flagi.ZF) == (UInt16)Flagi.ZF); }
			}
			public bool Flaga_SF    // sign flag
			{
				get { return ((flags & (UInt16)Flagi.SF) == (UInt16)Flagi.SF); }
			}
			/*
			public bool Flaga_OF    // overflaw flag
			{
				get { return ((flags & (UInt16)Flagi.OF) == (UInt16)Flagi.OF); }
			}
			*/

			public static bool CzyRejestr8Bitowy(string rejestr)
			{
				switch (rejestr.ToUpper())
				{
					case "AH": 
					case "AL": 
					case "BH": 
					case "BL": 
					case "CH": 
					case "CL": 
					case "DH": 
					case "DL":
						return true;
					default:
						return false;
				}
			}
			public byte Wartość8bit(string rejestr8)
			{
				switch (rejestr8.ToUpper())
				{
					case "AH": return (byte)(AX >> 8);
					case "AL": return (byte)(AX & 0xFF);
					case "BH": return (byte)(BX >> 8);
					case "BL": return (byte)(BX & 0xFF);
					case "CH": return (byte)(CX >> 8);
					case "CL": return (byte)(CX & 0xFF);
					case "DH": return (byte)(DX >> 8);
					case "DL": return (byte)(DX & 0xFF);
					default: throw new ArgumentOutOfRangeException(rejestr8);
				}
			}

			public UInt16 Wartość16bit(string rejestr16)
			{
				switch (rejestr16.ToUpper())
				{
					case "AX": return AX;
					case "BX": return BX;
					case "CX": return CX;
					case "DX": return DX;
					default: throw new ArgumentOutOfRangeException(rejestr16);
				}
			}
			public void UstawFlagi16(UInt32 wynik32) 
			 {
				bool cf, pf, zf, sf;
				
				// wynik bez przeniesienia
				UInt16 wynik16 = (UInt16)(wynik32 & 0xFFFF);
				
				// Carry flag
				cf = (wynik32 != wynik16);
				// In x86 processors, the parity flag reflects the parity only of the least significant byte of the result
				int liczba_ustawionych_bitów = 0;
				byte ostatni_bajt = (byte)(wynik32 & 0xFF);
				for (int i = 1; i <= 8; i++)
				{
					if ((ostatni_bajt & 0x1) != 0) liczba_ustawionych_bitów++;
					ostatni_bajt /= 2;
				}
				pf = ((liczba_ustawionych_bitów % 2) == 0);
				// Zero flag
				zf = (wynik16 == 0);
				// Sign flag - ustawiony najstarszy bit
				sf = ((wynik16 & 0b1000000000000000) != 0);
				//of = false;
				UstawFlagi(cf, pf, zf, sf);
			}

			public void UstawFlagi8(UInt16 wynik16)
			{
				bool cf, pf, zf, sf;

				// wynik bez przeniesienia
				byte wynik8 = (byte)(wynik16 & 0xFF);

				// Carry flag
				cf = (wynik16 != wynik8);
				// In x86 processors, the parity flag reflects the parity only of the least significant byte of the result
				int liczba_ustawionych_bitów = 0;
				byte ostatni_bajt = wynik8;
				for (int i = 1; i <= 8; i++)
				{
					if ((ostatni_bajt & 0x1) != 0) liczba_ustawionych_bitów++;
					ostatni_bajt /= 2;
				}
				pf = ((liczba_ustawionych_bitów % 2) == 0);
				// Zero flag
				zf = (wynik8 == 0);
				// Sign flag - ustawiony najstarszy bit
				sf = ((wynik8 & 0b10000000) != 0);
				//of = false;
				UstawFlagi(cf, pf, zf, sf);
			}

			public int WyliczAdresEfektywny(string adres)
			{
				string adres_ = adres.Trim();
				// adres bez nawiasów kwadratowych
				adres_ = adres_.Substring(1, adres_.Length - 2);

				int start = 0; // start wyszukiwania separatorow
				char[] separatory = new char[] { '+', '-' };
				string s;
				char znak;
				int wartość = 0;
				int x = 0;

				for( ; ; )
				{
					// gdzie nastęny znak +/-?
					int n = adres_.IndexOfAny(separatory, start + 1);

					// zakładam, że pierwszy bez znaku: [AX + DX + 123]
					if (start == 0)
					{
						znak = '+';
						if (n == -1)
							s = adres_;
						else
							s = adres_.Substring(0, n).Trim();
					}
					else
					{
						znak = adres_[start];

						if (n != -1)
						{
							// składnik - od nzastępnego char po poprzednim znaku +/-
							s = adres_.Substring(start + 1, n - start - 1).Trim();
						}
						else
						{
							// ostatni składnik
							s = adres_.Substring(start + 1).Trim();
						}
					}

					// kontrola 
					switch (s.ToUpper())
					{
						case "AX": x = AX; break;
						case "BX": x = BX; break;
						case "CX": x = CX; break;
						case "DX": x = DX; break;

						case "AH": x = AH; break;
						case "AL": x = AL; break;
						case "BH": x = BH; break;
						case "BL": x = BL; break;
						case "CH": x = CL; break;
						case "CL": x = CL; break;
						case "DH": x = DH; break;
						case "DL": x = DL; break;

						default:
							if (!int.TryParse(s, out x))
								throw new InvalidDataException($"Nieprawidłowy składnik adresu: {s}");
							break;
					}
					// dodaj do adresu
					wartość = wartość + (znak == '-' ? -1 : 1) * x;

					if (n == -1) 
						break;
					// nastęna analiza od znaku +/-
					start = n;
				}

				return wartość;
			}
		}

		public class Operand
		{
			public enum RodzajTyp
			{
				Brak,
				Rejestr,
				Liczba,
				//Etykieta,
				Adres
			}
			public RodzajTyp Rodzaj = RodzajTyp.Brak;
			public string WartośćStr = string.Empty;
			public UInt16 Wartość = 0;

			public Operand(string s)
			{
				string s_ = s.ToUpper();

				if (s == "")
				{
					Rodzaj = RodzajTyp.Brak;
				}
				else if ((s_.Length > 2) && (s_[0] == '[') && (s_[s_.Length - 1] == ']'))
				{
					Rodzaj = RodzajTyp.Adres;
					WartośćStr = s_;
				}
				else
				{
					switch (s_)
					{
						case "AX":
						case "BX":
						case "CX":
						case "DX":
							Rodzaj = RodzajTyp.Rejestr;
							WartośćStr = s_;
							break;

						case "AH":
						case "AL":
						case "BH":
						case "BL":
						case "CH":
						case "CL":
						case "DH":
						case "DL":
							Rodzaj = RodzajTyp.Rejestr;
							WartośćStr = s_;
							break;

						default:
							if (UInt16.TryParse(s, out UInt16 wartość))
							{
								Rodzaj = RodzajTyp.Liczba;
								Wartość = wartość;
							}
							else
							{
								//Rodzaj = RodzajTyp.Etykieta;
								//WartośćStr = s;
								throw new InvalidDataException($"Nieprawidłowy operand: {s}");
							}
							break;
					}
				}
			}

			public static Operand Brak { get { return new Operand(""); } }
		}

		public class Rozkaz
		{
			public string Mnemonik;
			public string LiniaŹródłowa;

			public Rozkaz(string mnemonik, string liniaŹródłowa)
			{ 
				Mnemonik = mnemonik;
				LiniaŹródłowa = liniaŹródłowa;
			}
			public virtual void Wykonaj(Rejestry rejestry, UInt16[] pamięć)
			{
				var kolor = Console.ForegroundColor;
				Console.ForegroundColor = Program.KolorInstrukcji;
				Console.Write($"{LiniaŹródłowa, -30}");
				Console.ForegroundColor = kolor;

				Console.WriteLine($"        CF= {rejestry.Flaga_CF}, PF= {rejestry.Flaga_PF}, ZF= {rejestry.Flaga_ZF}, SF= {rejestry.Flaga_SF}, AX= {rejestry.AX}, BX= {rejestry.BX}, CX= {rejestry.CX}, DX= {rejestry.DX}");
			}
		}

		public class RozkazNOP : Rozkaz
		{
			public RozkazNOP(string mnemonik, string liniaŹródłowa) : base(mnemonik, liniaŹródłowa)
			{
			}
			public override void Wykonaj(Rejestry rejestry, UInt16[] pamięć)
			{
				base.Wykonaj(rejestry, pamięć);
			}
		}
		public class RozkazADD : Rozkaz
		{
			Operand op1;
			Operand op2;

			public RozkazADD(string mnemonik, string liniaŹródłowa, Operand op1, Operand op2) : base(mnemonik, liniaŹródłowa)
			{
				// op1 musi być rejestrem, op2 nie może być pusty i nie może być etykietą w kodzie
				if (op1.Rodzaj == Operand.RodzajTyp.Brak)
					throw new ArgumentException($"Brak pierwszego operanda", "op1");
				if (op1.Rodzaj != Operand.RodzajTyp.Rejestr)
					throw new ArgumentException($"Pierwszy operand musi być rejestrem", "op1");
				if (op2.Rodzaj == Operand.RodzajTyp.Brak)
					throw new ArgumentException($"Brak drugiego operanda", "op2");
				this.op1 = op1;
				this.op2 = op2;
			}
			public override void Wykonaj(Rejestry rejestry, UInt16[] pamięć)
			{
				UInt32 wynik32 = 0; // wynik dla operacji na pełnych rejestrach (16 bit)
				UInt16 wynik16 = 0; // wynik dla operacji na bajtach

				// wartość z operand 2: 
				UInt16 wartość2 = 0;
				switch (op2.Rodzaj)
				{
					case Operand.RodzajTyp.Liczba:
						wartość2 = op2.Wartość;
						break;
					case Operand.RodzajTyp.Rejestr:
						wartość2 = rejestry.Wartość16bit(op2.WartośćStr);
						break;
					case Operand.RodzajTyp.Adres:
						wartość2 = pamięć[rejestry.WyliczAdresEfektywny(op2.WartośćStr)];
						break;
				}

				// dodaj do operand1: rejestr 
				switch (op1.Rodzaj)
				{
					case Operand.RodzajTyp.Rejestr:
						switch (op1.WartośćStr)
						{
							case "AX": 
								wynik32 = (UInt32)(rejestry.AX + wartość2);
								rejestry.AX = (UInt16)(wynik32 & 0xFFFF);
								rejestry.UstawFlagi16(wynik32);  
								break;
							case "BX": 
								wynik32 = (UInt32)(rejestry.BX + wartość2);
								rejestry.BX = (UInt16)(wynik32 & 0xFFFF);
								rejestry.UstawFlagi16(wynik32); 
								break;
							case "CX": 
								wynik32 = (UInt32)(rejestry.CX + wartość2);
								rejestry.CX = (UInt16)(wynik32 & 0xFFFF); 
								rejestry.UstawFlagi16(wynik32); 
								break;
							case "DX": 
								wynik32 = (UInt32)(rejestry.DX + wartość2);
								rejestry.DX = (UInt16)(wynik32 & 0xFFFF); 
								rejestry.UstawFlagi16(wynik32); 
								break;

							case "AH": 
								wynik16 = (UInt16)(rejestry.AH + (byte)(wartość2 & 0xFF));
								rejestry.AH = (byte)(wynik16 & 0xFF);
								rejestry.UstawFlagi8(wynik16); 
								break;
							case "AL": 
								wynik16 = (UInt16)(rejestry.AL + (byte)(wartość2 & 0xFF));
								rejestry.AL = (byte)(wynik16 & 0xFF); 
								rejestry.UstawFlagi8(wynik16); 
								break;
							case "BH": 
								wynik16 = (UInt16)(rejestry.BH + (byte)(wartość2 & 0xFF));
								rejestry.BH = (byte)(wynik16 & 0xFF); 
								rejestry.UstawFlagi8(wynik16); 
								break;
							case "BL": 
								wynik16 = (UInt16)(rejestry.BL + (byte)(wartość2 & 0xFF));
								rejestry.BL = (byte)(wynik16 & 0xFF);
								rejestry.UstawFlagi8(wynik16); 
								break;
							case "CH": 
								wynik16 = (UInt16)(rejestry.CH + (byte)(wartość2 & 0xFF));
								rejestry.CH = (byte)(wynik16 & 0xFF); 
								rejestry.UstawFlagi8(wynik16); 
								break;
							case "CL": 
								wynik16 = (UInt16)(rejestry.CL + (byte)(wartość2 & 0xFF));
								rejestry.CL = (byte)(wynik16 & 0xFF); 
								rejestry.UstawFlagi8(wynik16); 
								break;
							case "DH": 
								wynik16 = (UInt16)(rejestry.DH + (byte)(wartość2 & 0xFF));
								rejestry.DH = (byte)(wynik16 & 0xFF);
								rejestry.UstawFlagi8(wynik16); 
								break;
							case "DL": 
								wynik16 = (UInt16)(rejestry.DL + (byte)(wartość2 & 0xFF));
								rejestry.DL = (byte)(wynik16 & 0xFF); 
								rejestry.UstawFlagi8(wynik16); 
								break;
						}
						break;
				}
				base.Wykonaj(rejestry, pamięć);
			}
		}
		public class RozkazSUB : Rozkaz
		{
			Operand op1;
			Operand op2;

			public RozkazSUB(string mnemonik, string liniaŹródłowa, Operand op1, Operand op2) : base(mnemonik, liniaŹródłowa)
			{
				// agr1 musi być rejestrem
				if (op1.Rodzaj != Operand.RodzajTyp.Rejestr)
					throw new ArgumentException($"Pierwszy operand musi być rejestrem", "op1");
				this.op1 = op1;
				this.op2 = op2;
			}
			public override void Wykonaj(Rejestry rejestry, UInt16[] pamięć)
			{
				UInt32 wynik32 = 0; // wynik dla operacji na pełnych rejestrach (16 bit)
				UInt16 wynik16 = 0; // wynik dla operacji na bajtach

				// wartość z operand 2: 
				UInt16 wartość2 = 0;
				switch (op2.Rodzaj)
				{
					case Operand.RodzajTyp.Liczba:
						wartość2 = op2.Wartość;
						break;
					case Operand.RodzajTyp.Rejestr:
						wartość2 = rejestry.Wartość16bit(op2.WartośćStr);
						break;
					case Operand.RodzajTyp.Adres:
						wartość2 = pamięć[rejestry.WyliczAdresEfektywny(op2.WartośćStr)];
						break;
				}

				// dodaj do operand1: rejestr 
				switch (op1.Rodzaj)
				{
					case Operand.RodzajTyp.Rejestr:
						switch (op1.WartośćStr)
						{
							case "AX":
								wynik32 = (UInt32)(rejestry.AX - wartość2);
								rejestry.AX = (UInt16)(wynik32 & 0xFFFF);
								rejestry.UstawFlagi16(wynik32);
								break;
							case "BX":
								wynik32 = (UInt32)(rejestry.BX - wartość2);
								rejestry.BX = (UInt16)(wynik32 & 0xFFFF);
								rejestry.UstawFlagi16(wynik32);
								break;
							case "CX":
								wynik32 = (UInt32)(rejestry.CX - wartość2);
								rejestry.CX = (UInt16)(wynik32 & 0xFFFF);
								rejestry.UstawFlagi16(wynik32);
								break;
							case "DX":
								wynik32 = (UInt32)(rejestry.DX - wartość2);
								rejestry.DX = (UInt16)(wynik32 & 0xFFFF);
								rejestry.UstawFlagi16(wynik32);
								break;

							case "AH":
								wynik16 = (UInt16)(rejestry.AH - (byte)(wartość2 & 0xFF));
								rejestry.AH = (byte)(wynik16 & 0xFF);
								rejestry.UstawFlagi8(wynik16);
								break;
							case "AL":
								wynik16 = (UInt16)(rejestry.AL - (byte)(wartość2 & 0xFF));
								rejestry.AL = (byte)(wynik16 & 0xFF);
								rejestry.UstawFlagi8(wynik16);
								break;
							case "BH":
								wynik16 = (UInt16)(rejestry.BH - (byte)(wartość2 & 0xFF));
								rejestry.BH = (byte)(wynik16 & 0xFF);
								rejestry.UstawFlagi8(wynik16);
								break;
							case "BL":
								wynik16 = (UInt16)(rejestry.BL - (byte)(wartość2 & 0xFF));
								rejestry.BL = (byte)(wynik16 & 0xFF);
								rejestry.UstawFlagi8(wynik16);
								break;
							case "CH":
								wynik16 = (UInt16)(rejestry.CH - (byte)(wartość2 & 0xFF));
								rejestry.CH = (byte)(wynik16 & 0xFF);
								rejestry.UstawFlagi8(wynik16);
								break;
							case "CL":
								wynik16 = (UInt16)(rejestry.CL - (byte)(wartość2 & 0xFF));
								rejestry.CL = (byte)(wynik16 & 0xFF);
								rejestry.UstawFlagi8(wynik16);
								break;
							case "DH":
								wynik16 = (UInt16)(rejestry.DH - (byte)(wartość2 & 0xFF));
								rejestry.DH = (byte)(wynik16 & 0xFF);
								rejestry.UstawFlagi8(wynik16);
								break;
							case "DL":
								wynik16 = (UInt16)(rejestry.DL - (byte)(wartość2 & 0xFF));
								rejestry.DL = (byte)(wynik16 & 0xFF);
								rejestry.UstawFlagi8(wynik16);
								break;
						}
						break;
				}
				base.Wykonaj(rejestry, pamięć);
			}
		}

		public class RozkazMOV : Rozkaz
		{
			Operand op1;
			Operand op2;
			public RozkazMOV(string mnemonik, string liniaŹródłowa, Operand op1, Operand op2) : base(mnemonik, liniaŹródłowa)
			{
				// agr1 musi być rejestrem lub adresem
				switch (op1.Rodzaj)
				{
					case Operand.RodzajTyp.Brak:
					case Operand.RodzajTyp.Liczba:
						throw new ArgumentException($"Pierwszy operand musi być rejestrem lub adresem", "op1");
				}
				// agr2 musi być rejestrem lub wartością lub adresem
				switch (op2.Rodzaj)
				{
					case Operand.RodzajTyp.Brak:
						throw new ArgumentException($"Drugi operand musi być rejestrem lub wartością", "op2");
				}
				// nie może być op1 i op2 jednocześnie adresem
				if ((op1.Rodzaj == Operand.RodzajTyp.Adres) && (op2.Rodzaj == Operand.RodzajTyp.Adres))
					throw new ArgumentException($"Nie mogę oba operandy być typu Adres", "op1");

				this.op1 = op1;
				this.op2 = op2;
			}
			public override void Wykonaj(Rejestry rejestry, UInt16[] pamięć)
			{
				// wartość z operand 2: 
				UInt16 wartość2 = 0;
				switch(op2.Rodzaj)
				{
					case Operand.RodzajTyp.Liczba:
						wartość2 = op2.Wartość;
						break;
					case Operand.RodzajTyp.Rejestr:
						wartość2 = rejestry.Wartość16bit(op2.WartośćStr);
						break;
					case Operand.RodzajTyp.Adres:
						wartość2 = pamięć[rejestry.WyliczAdresEfektywny(op2.WartośćStr)];
						break;
				}

				// zapisz do operand1: rejestr lub pamięć
				int adres1 = 0;
				switch (op1.Rodzaj)
				{
					case Operand.RodzajTyp.Adres:
						adres1 = rejestry.WyliczAdresEfektywny(op1.WartośćStr);
						pamięć[adres1] = wartość2;
						break;
					case Operand.RodzajTyp.Rejestr:
						switch(op1.WartośćStr)
						{
							case "AX": rejestry.AX = wartość2; break;
							case "BX": rejestry.BX = wartość2; break;
							case "CX": rejestry.CX = wartość2; break;
							case "DX": rejestry.DX = wartość2; break;
							case "AH": rejestry.AH = (byte)(wartość2 & 0xFF); break;
							case "AL": rejestry.AL = (byte)(wartość2 & 0xFF); break;
							case "BH": rejestry.BH = (byte)(wartość2 & 0xFF); break;
							case "BL": rejestry.BL = (byte)(wartość2 & 0xFF); break;
							case "CH": rejestry.CH = (byte)(wartość2 & 0xFF); break;
							case "CL": rejestry.CL = (byte)(wartość2 & 0xFF); break;
							case "DH": rejestry.DH = (byte)(wartość2 & 0xFF); break;
							case "DL": rejestry.DL = (byte)(wartość2 & 0xFF); break;
						}
						break;
				}
				base.Wykonaj(rejestry, pamięć);
			}
		}

		// XCHG - nie ustawia znaczników
		public class RozkazXCHG : Rozkaz
		{
			Operand op1;
			Operand op2;
			public RozkazXCHG(string mnemonik, string liniaŹródłowa, Operand op1, Operand op2) : base(mnemonik, liniaŹródłowa)
			{
				// agr1 musi być rejestrem lub adresem
				switch (op1.Rodzaj)
				{
					case Operand.RodzajTyp.Rejestr:
					case Operand.RodzajTyp.Adres:
						break;
					default:
						throw new ArgumentException($"Pierwszy operand musi być rejestrem lub adresem", "op1");
				}
				// agr2 musi być rejestrem lub adresem
				switch (op2.Rodzaj)
				{
					case Operand.RodzajTyp.Rejestr:
					case Operand.RodzajTyp.Adres:
						break;
					default:
						throw new ArgumentException($"Drugi operand musi być rejestrem lub adresem", "op2");
				}
				// nie może być op1 i op2 jednocześnie adresem
				if ((op1.Rodzaj == Operand.RodzajTyp.Adres) && (op2.Rodzaj == Operand.RodzajTyp.Adres))
					throw new ArgumentException($"Nie mogę oba operandy być typu Adres", "op1");

				// rejestry muszą być tej samej długości
				if (Rejestry.CzyRejestr8Bitowy(op1.WartośćStr) ^ Rejestry.CzyRejestr8Bitowy(op2.WartośćStr))
					throw new ArgumentException($"Rejestry muszą mieć te same długości", "op2");

				this.op1 = op1;
				this.op2 = op2;
			}
			public override void Wykonaj(Rejestry rejestry, UInt16[] pamięć)
			{
				// wartość z operand 1: 
				UInt16 wartość1 = 0;
				int adres1 = 0;
				switch (op1.Rodzaj)
				{
					case Operand.RodzajTyp.Rejestr:
						wartość1 = rejestry.Wartość16bit(op1.WartośćStr);
						break;
					case Operand.RodzajTyp.Adres:
						adres1 = rejestry.WyliczAdresEfektywny(op1.WartośćStr);
						wartość1 = pamięć[adres1];
						break;
				}
				// wartość z operand 2: 
				UInt16 wartość2 = 0;
				int adres2 = 0;
				switch (op2.Rodzaj)
				{
					case Operand.RodzajTyp.Rejestr:
						wartość2 = rejestry.Wartość16bit(op2.WartośćStr);
						break;
					case Operand.RodzajTyp.Adres:
						adres2 = rejestry.WyliczAdresEfektywny(op2.WartośćStr);
						wartość2 = pamięć[adres2];
						break;
				}

				// zapisz do operand2
				switch(op2.Rodzaj)
				{
					case Operand.RodzajTyp.Rejestr:
						switch(op2.WartośćStr)
						{
							case "AX": rejestry.AX = wartość1; break;
							case "BX": rejestry.BX = wartość1; break;
							case "CX": rejestry.CX = wartość1; break;
							case "DX": rejestry.DX = wartość1; break;
							case "AH": rejestry.AH = (byte)(wartość1 & 0xFF); break;
							case "AL": rejestry.AL = (byte)(wartość1 & 0xFF); break;
							case "BH": rejestry.BH = (byte)(wartość1 & 0xFF); break;
							case "BL": rejestry.BL = (byte)(wartość1 & 0xFF); break;
							case "CH": rejestry.CH = (byte)(wartość1 & 0xFF); break;
							case "CL": rejestry.CL = (byte)(wartość1 & 0xFF); break;
							case "DH": rejestry.DH = (byte)(wartość1 & 0xFF); break;
							case "DL": rejestry.DL = (byte)(wartość1 & 0xFF); break;
						}
						break;
					case Operand.RodzajTyp.Adres:
						pamięć[adres2] = wartość1;
						break;
				}
				// zapisz do operand1: rejestr lub pamięć
				switch (op1.Rodzaj)
				{
					case Operand.RodzajTyp.Rejestr:
						switch (op1.WartośćStr)
						{
							case "AX": rejestry.AX = wartość2; break;
							case "BX": rejestry.BX = wartość2; break;
							case "CX": rejestry.CX = wartość2; break;
							case "DX": rejestry.DX = wartość2; break;
							case "AH": rejestry.AH = (byte)(wartość2 & 0xFF); break;
							case "AL": rejestry.AL = (byte)(wartość2 & 0xFF); break;
							case "BH": rejestry.BH = (byte)(wartość2 & 0xFF); break;
							case "BL": rejestry.BL = (byte)(wartość2 & 0xFF); break;
							case "CH": rejestry.CH = (byte)(wartość2 & 0xFF); break;
							case "CL": rejestry.CL = (byte)(wartość2 & 0xFF); break;
							case "DH": rejestry.DH = (byte)(wartość2 & 0xFF); break;
							case "DL": rejestry.DL = (byte)(wartość2 & 0xFF); break;
						}
						break;
					case Operand.RodzajTyp.Adres:
						pamięć[adres1] = wartość2;
						break;
				}
				base.Wykonaj(rejestry, pamięć);
			}
		}
		
		static int ZnajdźEtykietę(List<LiniaProgramu> program, string etykieta)
		{
			var wynik = program.Where(linia => linia.Etykieta == etykieta).FirstOrDefault();
			if (wynik != null)
				return program.IndexOf(wynik);
			else
				throw new ArgumentOutOfRangeException(etykieta, $"Brak etykiety: {etykieta} w kodzie programu");
		}

		static void Main(string[] args)
		{
			var rejestry = new Rejestry();            
			var pamięć = new UInt16[ROZMIAR_PAMIĘCI];


			Console.WriteLine("---- Program.txt - 20 liczb Fibonacciego ------------------------------------------------------");            
			var linieProgramu = ZaładujProgram(NazwaPliku);
			Console.WriteLine($"Liczba linii programu= {linieProgramu.Count}");

			Console.WriteLine("");
			Console.WriteLine("---- WYKONANIE PROGRAMU -----------------------------------------------------------------------");

			rejestry.IP = 0;
			do
			{
				var linia = linieProgramu[rejestry.IP];
				var mnemonik = linia.Instrukcja.ToUpper();
				Operand op1 = Operand.Brak;
				Operand op2 = Operand.Brak;
				Rozkaz rozkaz = null;

				if (mnemonik == "RET")
					break;
				else
				{
					op1 = new Operand(linia.Op1);
					op2 = new Operand(linia.Op2);

					switch (mnemonik)
					{
						case "ADD":
							rozkaz = new RozkazADD(mnemonik,linia.LiniaŹródłowa, op1, op2);
							break;
						case "SUB":
							rozkaz = new RozkazSUB(mnemonik, linia.LiniaŹródłowa, op1, op2);
							break;
						case "MOV":
							rozkaz = new RozkazMOV(mnemonik, linia.LiniaŹródłowa, op1, op2);
							break;
						case "XCHG":
							rozkaz = new RozkazXCHG(mnemonik, linia.LiniaŹródłowa, op1, op2);
							break;
						case "NOP":
							break;
						default:
							// przerwanie programu
							throw new ArgumentOutOfRangeException(mnemonik, $"Nieznany mnemonik: {mnemonik}!");
					}

					if (rozkaz != null)
					{
						rozkaz.Wykonaj(rejestry, pamięć);
					}

				}
				rejestry.IP++;
			} while (true);

			Console.WriteLine(""); 
			Console.WriteLine("---- ZRZUT PAMIĘCI ----------------------------------------------------------------------------");

			for (int adres = 0; adres < 20; adres++)
				Console.WriteLine($"mem[{adres}]= {pamięć[adres]}");
			Console.WriteLine($"...");

			Console.WriteLine("-----------------------------------------------------------------------------------------------");

			Console.WriteLine(""); 
			Console.WriteLine("Koniec. Naciśnij coś.");
			Console.ReadKey();

		}


		static List<LiniaProgramu> ZaładujProgram(string nazwaPliku)
		{
			string linia; 

			var linieProgramu = new List<LiniaProgramu>();
			// ^ (?:(\w+):)? \s* (\w{3,4}) (?:\s+(\w+))? (?:,(\w+))? $
			var wzorzec = @"^(?:(\w+):)?\s*(\w{3,4})(?:\s+(\w+|\[.+\]))?(?:,(\w+|\[.+\]))?$";
			var regEx = new System.Text.RegularExpressions.Regex(wzorzec);

			StreamReader file = new System.IO.StreamReader(nazwaPliku);
			while ((linia = file.ReadLine()) != null)
			{
				linia = linia.Trim();
				var dopasowanie = regEx.Match(linia);

				if (dopasowanie.Success)
				{
					//Console.WriteLine($"----------------------------------------");
					//Console.WriteLine($"{linia}");
					//program.Add(new LiniaProgramu())
					//Console.WriteLine($"0: {dopasowanie.Groups[0]}, 1: {dopasowanie.Groups[1]}, 2: {dopasowanie.Groups[2]}, 3: {dopasowanie.Groups[3]}, 4: {dopasowanie.Groups[4]}");

					linieProgramu.Add(new LiniaProgramu(dopasowanie.Groups[1].Value, dopasowanie.Groups[2].Value, dopasowanie.Groups[3].Value, dopasowanie.Groups[4].Value, linia));
				}
				else
				{
					throw new InvalidDataException($"Linia w nieprawidłowym formacie: {linia}");
				}
			}

			file.Close();

			return linieProgramu;
		}
	}
}
