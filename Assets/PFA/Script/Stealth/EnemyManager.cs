using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton qui garde la liste de tous les ennemis actifs.
/// Sert de point central pour diffuser une alerte (rayon rouge) aux autres ennemis.
/// </summary>
public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance { get; private set; }

    private readonly List<EnemyAI> _enemies = new List<EnemyAI>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void Register(EnemyAI enemy)
    {
        if (!_enemies.Contains(enemy))
            _enemies.Add(enemy);
    }

    public void Unregister(EnemyAI enemy)
    {
        _enemies.Remove(enemy);
    }

    public IReadOnlyList<EnemyAI> AllEnemies => _enemies;

    public IEnumerable<EnemyAI> GetOtherEnemies(EnemyAI excluding)
    {
        foreach (var e in _enemies)
        {
            if (e != excluding)
                yield return e;
        }
    }
}
