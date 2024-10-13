using Soneta.Business;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Soneta.Kadry;
using Soneta.KadryPlace;
using Soneta.Types;
using Rekrutacja.Workers.Template;
using System.Text.RegularExpressions;

//Rejetracja Workera - Pierwszy TypeOf określa jakiego typu ma być wyświetlany Worker, Drugi parametr wskazuje na jakim Typie obiektów będzie wyświetlany Worker
[assembly: Worker(typeof(TemplateWorker), typeof(Pracownicy))]
namespace Rekrutacja.Workers.Template
{
    public class TemplateWorker
    {
        public enum Figury
        {
            Kwadrat,
            Prostokąt,
            Trójkąt,
            Koło
        }
        //Aby parametry działały prawidłowo dziedziczymy po klasie ContextBase
        public class TemplateWorkerParametry : ContextBase
        {

            [Caption("A")]
            public string A { get; set; }

            [Caption("B")]
            public string B { get; set; }

            [Caption("Data obliczeń")]
            public Date DataObliczen { get; set; }

            [Caption("Operacja")]
            public Figury Figura { get; set; }

            public TemplateWorkerParametry(Context context) : base(context)
            {
                this.DataObliczen = Date.Today;
            }
        }
        //Obiekt Context jest to pudełko które przechowuje Typy danych, aktualnie załadowane w aplikacji
        //Atrybut Context pobiera z "Contextu" obiekty które aktualnie widzimy na ekranie
        [Context]
        public Context Cx { get; set; }
        //Pobieramy z Contextu parametry, jeżeli nie ma w Context Parametrów mechanizm sam utworzy nowy obiekt oraz wyświetli jego formatkę
        [Context]
        public TemplateWorkerParametry Parametry { get; set; }
        //Atrybut Action - Wywołuje nam metodę która znajduje się poniżej
        [Action("Kalkulator",
           Description = "Prosty kalkulator ",
           Priority = 10,
           Mode = ActionMode.ReadOnlySession,
           Icon = ActionIcon.Accept,
           Target = ActionTarget.ToolbarWithText)]

        public void WykonajAkcje()
        {
            //Włączenie Debug, aby działał należy wygenerować DLL w trybie DEBUG
            DebuggerSession.MarkLineAsBreakPoint();
            //Pobieranie danych z Contextu
            Pracownik[] pracownicy = null;
            if (this.Cx.Contains(typeof(Pracownik[])))
            {
                pracownicy = (Pracownik[])this.Cx[typeof(Pracownik[])];
            }
            //Sprawdzenie czy context zawiera element typu Pracownik[]
            if (pracownicy == null)
            {
                throw new ArgumentNullException("Kontekst nie zawiera żadnego pracownika");
            }

            //Modyfikacja danych
            //Aby modyfikować dane musimy mieć otwartą sesję, któa nie jest read only
            using (Session nowaSesja = this.Cx.Login.CreateSession(false, false, "ModyfikacjaPracownika"))
            {
                //Otwieramy Transaction aby można było edytować obiekt z sesji
                using (ITransaction trans = nowaSesja.Logout(true))
                {
                    foreach (var pracownik in pracownicy)
                    {
                        //Pobieramy obiekt z Nowo utworzonej sesji
                        var pracownikZSesja = nowaSesja.Get(pracownik);

                        if (Parametry == null)
                        {
                            throw new ArgumentNullException("Parametry nie zostały przekazane");
                        }
                        int A = KonwersjaStringDoInt(this.Parametry.A);
                        int B = 0; // Domyślnie 0, gdy figura nie wymaga drugiego wymiaru
                        if (this.Parametry.Figura is Figury.Trójkąt || this.Parametry.Figura is Figury.Prostokąt)
                        {
                            B = KonwersjaStringDoInt(this.Parametry.B);
                        }
                        var wynik = ObliczPoleFigury(A, B, this.Parametry.Figura);
                        //Features - są to pola rozszerzające obiekty w bazie danych, dzięki czemu nie jestesmy ogarniczeni to kolumn jakie zostały utworzone przez producenta
                        pracownikZSesja.Features["DataObliczen"] = this.Parametry.DataObliczen;
                        //Pole Wynik przyjmuje wartość double, dlatego konieczna jest ponowna konwersja na typ double
                        double wynikDouble = wynik;
                        pracownikZSesja.Features["Wynik"] = wynikDouble;
                    }
                    //Zatwierdzamy zmiany wykonane w sesji
                    trans.CommitUI();
                }
                //Zapisujemy zmiany
                nowaSesja.Save();
            }
        }

        public int ObliczPoleFigury(double a, double b, Figury figura)
        {
            double wynik = 0;
            switch (figura)
            {
                case Figury.Kwadrat:
                    wynik = a * a;
                    break;
                case Figury.Trójkąt:
                    wynik = (a * b) / 2;
                    break;
                case Figury.Prostokąt:
                    wynik = a * b;
                    break;
                case Figury.Koło:
                    wynik = Math.PI * a * a;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("Wybrano niedozwoloną figurę");
            }
            //Wynik ma być typu INT dlatego wykonujemy konwersję
            return Convert.ToInt32(wynik);
        }

        public int KonwersjaStringDoInt(string zmienna)
        {
            int wynik = 0;
            bool liczbaDziesietna = false;
            int licznikSeparatorow = 0;
            int liczbaPoPrzecinku = 0;

            WalidacjaString(zmienna);
            for (int i = 0; i < zmienna.Length; i++)
            {
                var aktualnyZnak = zmienna[i];

                if ((aktualnyZnak == '.' || aktualnyZnak == ',') && licznikSeparatorow == 0)
                {
                    licznikSeparatorow++;
                    liczbaDziesietna = true;
                    continue;
                }

                int aktualnaLiczba = 0;
                if (liczbaDziesietna)
                {
                    liczbaPoPrzecinku++;
                    if (liczbaPoPrzecinku == 1) //Zakładamy, że parser zaokrągla do jednej liczby po przecinku
                    {
                        aktualnyZnak = ((aktualnyZnak >= '6') ? '1' : '0'); //Zaokrąglenie liczby dziesiętnej w górę lub dół w zależności od liczby po przecinku
                        aktualnaLiczba = aktualnyZnak - '0';
                        wynik = wynik + aktualnaLiczba;
                        break;
                    }
                }
                else
                {
                    aktualnaLiczba = aktualnyZnak - '0';
                    wynik = wynik * 10 + aktualnaLiczba;
                }
            }
            return wynik;
        }
        public void WalidacjaString(string zmienna)
        {
            if (System.String.IsNullOrEmpty(zmienna)) throw new ArgumentException("Zmienna nie może być pusta");
            if (zmienna[0] == '-') throw new FormatException("Zmienna do obliczania pola powierzchni figur nie może być ujemna");
            string wzorzec = @"^\d+([.,]\d+)?$";
            if (!Regex.IsMatch(zmienna, wzorzec)) throw new FormatException("Nieprawidłowy znak w zmiennej tekstowej");
        }
    }
}
