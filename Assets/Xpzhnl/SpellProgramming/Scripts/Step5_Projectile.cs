// ==================================================
// 第五步：投射物脚本
// ==================================================
// 这是在游戏世界中飞行的实际物体

using UnityEngine;

namespace SpellSystem
{
    /// <summary>
    /// 投射物组件 - 附加到投射物游戏对象上
    /// </summary>
    [AddComponentMenu("Spell System/Projectile")]
    public class Step5_Projectile : MonoBehaviour
    {
        // ========== 基础引用 ==========
        private GameObject caster;           // 谁发射的这个投射物
        private Rigidbody2D rb;             // 物理组件
        private SpellStats stats;           // 投射物的属性

        // ========== 运行时数据 ==========
        private Vector2 direction;          // 飞行方向
        private float aliveTime;            // 已存活时间

        /// <summary>
        /// 初始化投射物
        /// </summary>
        public void Initialize(GameObject caster, SpellStats stats, Vector2 direction)
        {
            this.caster = caster;
            this.stats = stats;
            this.direction = direction.normalized;
            this.aliveTime = 0f;

            // 获取物理组件
            rb = GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody2D>();
                rb.gravityScale = 0; // 默认不受重力影响
            }

            // 设置初始速度
            rb.velocity = direction * stats.speed;

            SpellLogger.LogProjectileInit(stats.damage, stats.speed, stats.lifetime, stats.piercing);
        }

        /// <summary>
        /// 每帧更新
        /// </summary>
        void Update()
        {
            // 计时
            aliveTime += Time.deltaTime;

            // 检查是否超过生命周期
            if (aliveTime >= stats.lifetime)
            {
                DestroyProjectile("生命周期结束");
            }
        }

        /// <summary>
        /// 碰撞检测
        /// </summary>
        void OnTriggerEnter2D(Collider2D other)
        {
            // 不要碰到自己
            if (other.gameObject == caster)
                return;

            // 不要碰到其他投射物
            if (other.GetComponent<Step5_Projectile>() != null)
            {
                return;
            }

            SpellLogger.LogHit(other.gameObject.name);

            // 造成伤害
            DealDamage(other.gameObject);

            // 减少穿透次数
            stats.piercing--;

            // 如果没有穿透了，销毁投射物
            if (stats.piercing < 0)
            {
                DestroyProjectile("穿透耗尽");
            }
        }

        /// <summary>
        /// 造成伤害
        /// </summary>
        void DealDamage(GameObject target)
        {
            SpellLogger.LogDamage(target.name, stats.damage);

            // TODO: 发送伤害事件给目标
            // target.GetComponent<Health>()?.TakeDamage(stats.damage);
        }

        /// <summary>
        /// 销毁投射物
        /// </summary>
        void DestroyProjectile(string reason = "")
        {
            SpellLogger.LogProjectileDestroy(reason);
            Destroy(gameObject);
        }
    }
}

