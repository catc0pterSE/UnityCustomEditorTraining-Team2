using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField] private string _name;
    [SerializeField] private float _maxHealth;
    [SerializeField] private float _health;
    [SerializeField] private bool _useRegeneration;
    [SerializeField] private float _regenerationPerSecond;
    [SerializeField] private float _damageResistance;
    [SerializeField] private float _speed;
    [SerializeField] private bool _boosted;
    [SerializeField] private float _boostSpeed;
    [SerializeField] private float _friction;
    [SerializeField] private Weapon _weapon;
    [SerializeField] private float _attackSpeed;
}
