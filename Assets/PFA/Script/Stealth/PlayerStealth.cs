using System;
using UnityEngine;

public class PlayerStealth : MonoBehaviour
{
    public static PlayerStealth Instance { get; private set; }

    public HidingLocker CurrentLocker { get; private set; }
    public bool IsHidden => CurrentLocker != null;

    /// Prévient les ennemis en cours de poursuite qu'un casier vient d'être occupé.
    public event Action<HidingLocker> OnPlayerHidden;
    public event Action OnPlayerExitedHiding;

    private void Awake()
    {
        Instance = this;
    }

    public void EnterLocker(HidingLocker locker)
    {
        if (locker == null || IsHidden) return;

        CurrentLocker = locker;
        locker.SetOccupied(true);
        OnPlayerHidden?.Invoke(locker);
    }

    public void ExitLocker()
    {
        if (!IsHidden) return;

        CurrentLocker.SetOccupied(false);
        CurrentLocker = null;
        OnPlayerExitedHiding?.Invoke();
    }
}
