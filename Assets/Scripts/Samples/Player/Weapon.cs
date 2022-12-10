using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Weapon
{
    [SerializeField] private float _damage;
    [SerializeField] private bool _poisoned;
    [SerializeField] private float _poisonDamage;
}