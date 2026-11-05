using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Wave", menuName = "Grid/Wave")]
public class WaveDefinition : ScriptableObject
{
    [System.Serializable]
    public class EnemyEntry
    {
        [SerializeField] private GridEnemySettings enemySettings;
        [SerializeField, Min(0)] private int amount;

        public GridEnemySettings EnemySettings => enemySettings;
        public int Amount => amount;

        internal void Validate()
        {
            amount = Mathf.Max(0, amount);
        }
    }

    [SerializeField] private string waveTitle = "Wave";
    [SerializeField, Min(0.01f)] private float duration = 30f;
    [SerializeField] private List<EnemyEntry> enemies = new List<EnemyEntry>();

    public string WaveTitle => waveTitle;
    public float Duration => duration;
    public IReadOnlyList<EnemyEntry> Enemies => enemies;

    private void OnValidate()
    {
        duration = Mathf.Max(0.01f, duration);
        if (enemies == null)
        {
            return;
        }

        foreach (EnemyEntry enemy in enemies)
        {
            if (enemy != null)
            {
                enemy.Validate();
            }
        }
    }
}
