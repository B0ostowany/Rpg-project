using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RPG.Movement;
using RPG.Core;
using System;
using RPG.Saving;
using RPG.Attributes;
using RPG.Stats;
using GameDevTV.Utils;

namespace RPG.Combat
{
    public class Fighter : MonoBehaviour, IAction, ISaveable, IModifierProvider
    {
        [SerializeField] float timeBetweenAttacks = 1.21f;
        [SerializeField] Weapon defaultWeapon = null;
        [SerializeField] string defaultWeaponName = "Unarmed";
        LazyValue<Weapon> currentWeapon = null;
        
        [SerializeField] Transform rightHandTransform = null;
        [SerializeField] Transform leftHandTransform = null;

        [SerializeField] bool isPlayer = false;
        Health target;
        Stamina stamina;
        private Mana mana;
        float timeSinceLastAttack=Mathf.Infinity;
        private void Awake()
        {
            currentWeapon = new LazyValue<Weapon>(SetupDefaultWeapon);
            stamina = GetComponent<Stamina>();
            mana = GetComponent<Mana>();
        }

        

        private void Start()
        {
            
            currentWeapon.ForceInit();
            
        }


        private void Update()
        {
            
            timeSinceLastAttack += Time.deltaTime;
            if (target == null) return;
            if (target.IsDead()) return;

            if (target != null && !IsInRange())
            {
                GetComponent<Mover>().MoveTo(target.transform.position,1f);
            }
            else
            {
                GetComponent<Mover>().Cancel();
                AttackBehaviour();
            }
        }
        private Weapon SetupDefaultWeapon()
        {
            AttachWeapon(defaultWeapon);
            return defaultWeapon;
        }
        private void AttackBehaviour()
        {
            
            transform.LookAt(target.transform);
            if (timeSinceLastAttack > timeBetweenAttacks &&stamina.GetStaminaValue()>=currentWeapon.value.GetStaminaCost()&&mana.GetManaValue()>=currentWeapon.value.GetManaCost())
            {
                //This will trigger the Hit() event
                TriggerAttack();
               
                timeSinceLastAttack = 0f;
                stamina.UseStamina(currentWeapon.value.GetStaminaCost());
                mana.UseMana(currentWeapon.value.GetManaCost());
                
            }
            else
            {
                stamina.UpdateStamina();
                mana.UpdateMana();
            }
        }

        private void TriggerAttack()
        {
            GetComponent<Animator>().ResetTrigger("stopAttack");
            GetComponent<Animator>().SetTrigger("attack");
        }

        //Animation Event
        private void Hit()
        {
            if(target == null) return;
            float damage = GetComponent<BaseStats>().GetStat(Stat.Damage);
            if (currentWeapon.value.HasProjectile())
            {
                currentWeapon.value.LaunchProejctile(rightHandTransform, leftHandTransform, target,gameObject,damage);
            }
            else
            {
               
                target.TakeDamage(gameObject, damage);
            }
            
        }

        private void Shoot()
        {
            Hit();
        }

        private bool IsInRange()
        {
            return Vector3.Distance(this.transform.position, target.transform.position) < currentWeapon.value.GetWeaponRange();
        }

        public void Attack(GameObject combatTarget)
        {
            
            GetComponent<ActionScheduler>().StartAction(this);
            target = combatTarget.GetComponent<Health>();
        }

        public Health GetTarget()
        {
            return target;
        }

        public void Cancel()
        {
            StopAttack();
            target = null;
            GetComponent<Mover>().Cancel();
        }

        private void StopAttack()
        {
            GetComponent<Animator>().ResetTrigger("attack");
            GetComponent<Animator>().SetTrigger("stopAttack");
        }
        public IEnumerable<float> GetAdditiveModifier(Stat stat)
        {
            if(stat ==Stat.Damage)
            {
                yield return currentWeapon.value.GetWeaponDamage();
            }
        }

        public IEnumerable<float> GetPercentageModifiers(Stat stat)
        {
            if(stat == Stat.Damage)
            {
                yield return currentWeapon.value.GetPercentageBonus();
            }
        }
        public bool CanAttack(GameObject combatTarget)
        {
            if (combatTarget == null) { return false; }
            Health targetToTest = combatTarget.GetComponent<Health>();
            return targetToTest != null && !targetToTest.IsDead();
        }
        public void EquipWeapon(Weapon weapon)
        {
            currentWeapon.value = weapon;
            AttachWeapon(weapon);
        }

        private void AttachWeapon(Weapon weapon)
        {
            Animator animator = GetComponent<Animator>();
            weapon.Spawn(rightHandTransform, leftHandTransform, animator);
        }

        

        public object CaptureState()
        {
            return currentWeapon.value.name;
        }

        public void RestoreState(object state)
        {
            string weaponName = (string)state;
            Weapon weapon = Resources.Load<Weapon>(weaponName); 
            EquipWeapon(weapon);
        }

        
    }
}

