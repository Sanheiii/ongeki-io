using MU3Input;

using System.Diagnostics.Metrics;

namespace MU3InputDebug
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Mu3IO.Init();
            while (true)
            {
                UpdateStates();
                Thread.Sleep(100);
            }
        }
        public static unsafe void UpdateStates()
        {
            byte left;
            byte right;
            byte opButton;
            short lever;
            Mu3IO.Poll();
            Mu3IO.GetGameButtons(&left, &right);
            Mu3IO.GetOpButtons(&opButton);
            Mu3IO.GetLever(&lever);
        }
    }
}
