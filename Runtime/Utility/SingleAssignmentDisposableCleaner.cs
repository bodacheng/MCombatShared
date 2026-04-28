using System.Collections.Generic;
using UniRx;

public class SingleAssignmentDisposableCleaner
{
    private static readonly List<SingleAssignmentDisposable> Disposables = new List<SingleAssignmentDisposable>();

    public static void Add(SingleAssignmentDisposable disposable)
    {
        if (!Disposables.Contains(disposable))
        {
            Disposables.Add(disposable);
        }
    }

    public static void Clear()
    {
        for (var i = 0; i < Disposables.Count; i++)
        {
            if (Disposables[i] != null && !Disposables[i].IsDisposed)
            {
                Disposables[i].Dispose();
            }
        }

        Disposables.Clear();
    }
}
