
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Test
{

    // 只写你游戏里实际会用到的buff，多一个都不要加
    public enum BuffType
    {
        DamageUp,    // 攻击+2
        DefenseUp,   // 受伤-1
        HealPerTurn, // 每回合回1血
        Stun,        // 眩晕，跳过回合
        Poison       // 每回合掉2血
    }

    // 角色类（玩家和敌人共用）
    public class Character
    {
        public int maxHp;
        public int currentHp;
        public int baseDamage;
        public Dictionary<BuffType, int> buffs = new Dictionary<BuffType, int>();

        public Character(int hp, int damage)
        {
            maxHp = hp;
            currentHp = hp;
            baseDamage = damage;
        }

        public void AddBuff(BuffType type, int turns)
        {
            if (buffs.ContainsKey(type)) buffs[type] += turns;
            else buffs.Add(type, turns);
        }

        public void RemoveBuff(BuffType type)
        {
            if (buffs.ContainsKey(type)) buffs.Remove(type);
        }

        // 回合开始时执行所有buff
        public bool OnTurnStart()
        {
            // 眩晕优先处理，直接跳过回合
            if (buffs.ContainsKey(BuffType.Stun))
            {
                buffs[BuffType.Stun]--;
                if (buffs[BuffType.Stun] <= 0) RemoveBuff(BuffType.Stun);
                return false; // 返回false表示跳过回合
            }

            if (buffs.ContainsKey(BuffType.HealPerTurn))
            {
                Heal(1);
                buffs[BuffType.HealPerTurn]--;
                if (buffs[BuffType.HealPerTurn] <= 0) RemoveBuff(BuffType.HealPerTurn);
            }

            if (buffs.ContainsKey(BuffType.Poison))
            {
                TakeDamage(2);
                buffs[BuffType.Poison]--;
                if (buffs[BuffType.Poison] <= 0) RemoveBuff(BuffType.Poison);
            }

            return true; // 返回true表示可以正常行动
        }

        // 回合结束时执行buff（这里暂时没有需要的）
        public void OnTurnEnd()
        {
            // 有需要再加
        }

        // 计算最终攻击伤害
        public int CalculateDamage()
        {
            int damage = baseDamage;
            if (buffs.ContainsKey(BuffType.DamageUp)) damage += 2;
            return damage;
        }

        // 受到伤害
        public void TakeDamage(int damage)
        {
            if (buffs.ContainsKey(BuffType.DefenseUp)) damage = Mathf.Max(1, damage - 1);
            currentHp = Mathf.Max(0, currentHp - damage);
        }

        // 回血
        public void Heal(int amount)
        {
            currentHp = Mathf.Min(maxHp, currentHp + amount);
        }
    }

    // 卡牌结构体
    public struct Card
    {
        public int id;
        public string name;
        public string description;

        public Card(int id, string name, string description)
        {
            this.id = id;
            this.name = name;
            this.description = description;
        }
    }

    // 游戏主管理器
    public class CardGameGJ : MonoBehaviour
    {
        // 游戏状态
        private enum GameState { Start, PlayerTurn, EnemyTurn, GameOver }
        private enum LogType { Init, UI, Deck, Card, Turn, Battle, Buff, Result }
        private GameState currentState;

        // 角色
        private Character player;
        private Character enemy;

        // 卡牌
        private List<Card> deck = new List<Card>();
        private List<Card> hand = new List<Card>();
        private List<Card> discard = new List<Card>();

        // UI元素（全部代码动态创建，不用手动拖）
        private Text playerHpText;
        private Text enemyHpText;
        private Text playerBuffText;
        private Text enemyBuffText;
        private Text gameInfoText;
        private RectTransform handPanel;
        private Button endTurnButton;
        private Button startButton;

        void Start()
        {
            // 初始化所有UI
            CreateAllUI();
            GameLog(LogType.UI, "UI创建完成");
            // 初始化游戏
            InitGame();
            currentState = GameState.Start;
            GameLog(LogType.Init, "游戏进入开始状态");
        }

        // 初始化游戏数据
        void InitGame()
        {
            GameLog(LogType.Init, "初始化游戏数据");
            // 创建玩家和敌人
            player = new Character(20, 3);
            enemy = new Character(15, 2);
            GameLog(LogType.Init, $"创建角色：玩家 {player.currentHp}/{player.maxHp} HP，敌人 {enemy.currentHp}/{enemy.maxHp} HP");

            // 初始化卡组（就10张卡，多一张都不要）
            deck.Clear();
            deck.Add(new Card(0, "普通攻击", "造成3点伤害"));
            deck.Add(new Card(0, "普通攻击", "造成3点伤害"));
            deck.Add(new Card(0, "普通攻击", "造成3点伤害"));
            deck.Add(new Card(1, "重击", "造成6点伤害"));
            deck.Add(new Card(2, "治疗", "恢复4点生命"));
            deck.Add(new Card(3, "力量", "获得2回合攻击+2"));
            deck.Add(new Card(4, "护盾", "获得2回合受伤-1"));
            deck.Add(new Card(5, "眩晕", "使敌人眩晕1回合"));
            deck.Add(new Card(6, "中毒", "使敌人中毒3回合"));
            deck.Add(new Card(7, "再生", "获得3回合每回合回1血"));
            GameLog(LogType.Deck, $"卡组创建完成，共 {deck.Count} 张牌");

            // 洗牌
            ShuffleDeck();
            // 清空手牌和弃牌堆
            hand.Clear();
            discard.Clear();
            GameLog(LogType.Card, "清空手牌和弃牌堆");
            // 抽初始手牌
            DrawCards(5);
            // 更新UI
            UpdateAllUI();
        }

        // 洗牌
        void ShuffleDeck()
        {
            for (int i = 0; i < deck.Count; i++)
            {
                Card temp = deck[i];
                int randomIndex = Random.Range(i, deck.Count);
                deck[i] = deck[randomIndex];
                deck[randomIndex] = temp;
            }
            GameLog(LogType.Deck, $"洗牌完成，当前牌库 {deck.Count} 张");
        }

        // 抽n张牌
        void DrawCards(int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (deck.Count == 0)
                {
                    // 牌库空了就把弃牌堆洗回去
                    GameLog(LogType.Deck, $"牌库为空，将弃牌堆 {discard.Count} 张洗回牌库");
                    deck.AddRange(discard);
                    discard.Clear();
                    ShuffleDeck();
                }
                if (deck.Count > 0)
                {
                    Card drawnCard = deck[0];
                    hand.Add(drawnCard);
                    deck.RemoveAt(0);
                    GameLog(LogType.Card, $"抽到卡牌：{drawnCard.name}，手牌 {hand.Count} 张，牌库剩余 {deck.Count} 张");
                }
            }
            // 更新手牌UI
            UpdateHandUI();
        }

        // 打出一张牌
        void PlayCard(int index)
        {
            if (currentState != GameState.PlayerTurn) return;

            Card card = hand[index];
            hand.RemoveAt(index);
            discard.Add(card);
            GameLog(LogType.Card, $"打出卡牌：{card.name}，手牌剩余 {hand.Count} 张，弃牌堆 {discard.Count} 张");

            // 所有卡牌效果硬编码在这里，不要分散
            switch (card.id)
            {
                case 0:
                    int normalDamage = player.CalculateDamage();
                    enemy.TakeDamage(normalDamage);
                    GameLog(LogType.Battle, $"普通攻击造成 {normalDamage} 点伤害，敌人生命 {enemy.currentHp}/{enemy.maxHp}");
                    break;
                case 1:
                    int heavyDamage = player.CalculateDamage() + 3;
                    enemy.TakeDamage(heavyDamage);
                    GameLog(LogType.Battle, $"重击造成 {heavyDamage} 点伤害，敌人生命 {enemy.currentHp}/{enemy.maxHp}");
                    break;
                case 2:
                    int playerHpBeforeHeal = player.currentHp;
                    player.Heal(4);
                    GameLog(LogType.Battle, $"玩家恢复 {player.currentHp - playerHpBeforeHeal} 点生命，当前 {player.currentHp}/{player.maxHp}");
                    break;
                case 3:
                    player.AddBuff(BuffType.DamageUp, 2);
                    GameLog(LogType.Buff, "玩家获得力量：攻击+2，持续2回合");
                    break;
                case 4:
                    player.AddBuff(BuffType.DefenseUp, 2);
                    GameLog(LogType.Buff, "玩家获得护盾：受伤-1，持续2回合");
                    break;
                case 5:
                    enemy.AddBuff(BuffType.Stun, 1);
                    GameLog(LogType.Buff, "敌人被眩晕，持续1回合");
                    break;
                case 6:
                    enemy.AddBuff(BuffType.Poison, 3);
                    GameLog(LogType.Buff, "敌人中毒，持续3回合");
                    break;
                case 7:
                    player.AddBuff(BuffType.HealPerTurn, 3);
                    GameLog(LogType.Buff, "玩家获得再生：每回合恢复1点生命，持续3回合");
                    break;
            }

            gameInfoText.text = $"你打出了：{card.name}";
            UpdateAllUI();
            CheckGameOver();
        }

        // 玩家结束回合
        void EndPlayerTurn()
        {
            if (currentState != GameState.PlayerTurn) return;

            player.OnTurnEnd();
            currentState = GameState.EnemyTurn;
            gameInfoText.text = "敌人回合";
            GameLog(LogType.Turn, "玩家回合结束，进入敌人回合");
            Invoke("EnemyTurn", 1f); // 延迟1秒，让玩家看清
        }

        // 敌人回合（最简单的随机出牌AI）
        void EnemyTurn()
        {
            // 先执行敌人的回合开始buff
            int enemyHpBeforeTurnStart = enemy.currentHp;
            bool canAct = enemy.OnTurnStart();
            LogTurnStartBuffResult("敌人", enemyHpBeforeTurnStart, enemy.currentHp, canAct);
            UpdateAllUI();

            if (CheckGameOver()) return;

            if (canAct)
            {
                // 敌人随机选择一个行动
                int action = Random.Range(0, 3);
                switch (action)
                {
                    case 0:
                    case 1:
                        player.TakeDamage(enemy.CalculateDamage());
                        gameInfoText.text = "敌人攻击了你！";
                        GameLog(LogType.Battle, $"敌人攻击玩家，玩家生命 {player.currentHp}/{player.maxHp}");
                        break;
                    case 2:
                        int enemyHpBeforeHeal = enemy.currentHp;
                        enemy.Heal(2);
                        gameInfoText.text = "敌人恢复了2点生命";
                        GameLog(LogType.Battle, $"敌人恢复 {enemy.currentHp - enemyHpBeforeHeal} 点生命，当前 {enemy.currentHp}/{enemy.maxHp}");
                        break;
                }
            }
            else
            {
                gameInfoText.text = "敌人被眩晕了，跳过回合";
                GameLog(LogType.Turn, "敌人被眩晕，跳过行动");
            }

            enemy.OnTurnEnd();
            UpdateAllUI();

            if (CheckGameOver()) return;

            // 敌人回合结束，开始玩家回合
            GameLog(LogType.Turn, "敌人回合结束，准备进入玩家回合");
            Invoke("StartPlayerTurn", 1f);
        }

        // 开始玩家回合
        void StartPlayerTurn()
        {
            currentState = GameState.PlayerTurn;
            GameLog(LogType.Turn, "玩家回合开始");
            // 执行玩家回合开始buff
            int playerHpBeforeTurnStart = player.currentHp;
            bool canAct = player.OnTurnStart();
            LogTurnStartBuffResult("玩家", playerHpBeforeTurnStart, player.currentHp, canAct);
            UpdateAllUI();

            if (CheckGameOver()) return;

            if (canAct)
            {
                DrawCards(1);
                gameInfoText.text = "你的回合，选择一张牌打出";
                GameLog(LogType.Turn, "玩家可以行动");
            }
            else
            {
                gameInfoText.text = "你被眩晕了，跳过回合";
                GameLog(LogType.Turn, "玩家被眩晕，跳过行动");
                Invoke("EndPlayerTurn", 1f);
            }
        }

        // 检查游戏是否结束
        bool CheckGameOver()
        {
            if (player.currentHp <= 0)
            {
                currentState = GameState.GameOver;
                gameInfoText.text = "你输了！点击开始按钮重新开始";
                startButton.gameObject.SetActive(true);
                GameLog(LogType.Result, "游戏结束：玩家失败");
                return true;
            }
            if (enemy.currentHp <= 0)
            {
                currentState = GameState.GameOver;
                gameInfoText.text = "你赢了！点击开始按钮重新开始";
                startButton.gameObject.SetActive(true);
                GameLog(LogType.Result, "游戏结束：玩家胜利");
                return true;
            }
            return false;
        }

        // 更新所有UI
        void UpdateAllUI()
        {
            playerHpText.text = $"玩家生命：{player.currentHp}/{player.maxHp}";
            enemyHpText.text = $"敌人生命：{enemy.currentHp}/{enemy.maxHp}";
            playerBuffText.text = $"玩家buff：{GetBuffString(player.buffs)}";
            enemyBuffText.text = $"敌人buff：{GetBuffString(enemy.buffs)}";
            UpdateHandUI();
        }

        // 获取buff显示字符串
        string GetBuffString(Dictionary<BuffType, int> buffs)
        {
            if (buffs.Count == 0) return "无";
            string s = "";
            foreach (var buff in buffs)
            {
                s += $"{buff.Key}({buff.Value}回合) ";
            }
            return s;
        }

        void LogTurnStartBuffResult(string targetName, int hpBefore, int hpAfter, bool canAct)
        {
            int hpChange = hpAfter - hpBefore;
            if (hpChange > 0)
            {
                GameLog(LogType.Buff, $"{targetName}回合开始 Buff 生效：恢复 {hpChange} 点生命");
            }
            else if (hpChange < 0)
            {
                GameLog(LogType.Buff, $"{targetName}回合开始 Buff 生效：受到 {-hpChange} 点伤害");
            }

            if (!canAct)
            {
                GameLog(LogType.Buff, $"{targetName}受到眩晕影响，无法行动");
            }
        }

        void GameLog(LogType type, string message)
        {
            Debug.Log($"<color={GetLogColor(type)}>[{GetLogLabel(type)}]</color> {message}");
        }

        string GetLogLabel(LogType type)
        {
            switch (type)
            {
                case LogType.Init: return "初始化";
                case LogType.UI: return "UI";
                case LogType.Deck: return "牌库";
                case LogType.Card: return "卡牌";
                case LogType.Turn: return "回合";
                case LogType.Battle: return "战斗";
                case LogType.Buff: return "Buff";
                case LogType.Result: return "结果";
                default: return "日志";
            }
        }

        string GetLogColor(LogType type)
        {
            switch (type)
            {
                case LogType.Init: return "#5DADE2";
                case LogType.UI: return "#95A5A6";
                case LogType.Deck: return "#F1C40F";
                case LogType.Card: return "#2ECC71";
                case LogType.Turn: return "#3498DB";
                case LogType.Battle: return "#E67E22";
                case LogType.Buff: return "#9B59B6";
                case LogType.Result: return "#E74C3C";
                default: return "white";
            }
        }

        // 更新手牌UI（修复销毁报错）
        void UpdateHandUI()
        {
            if (handPanel == null)
            {
                return;
            }

            List<Transform> oldCards = new List<Transform>();
            for (int i = handPanel.childCount - 1; i >= 0; i--)
            {
                oldCards.Add(handPanel.GetChild(i));
            }

            foreach (Transform oldCard in oldCards)
            {
                if (oldCard != null)
                {
                    Destroy(oldCard.gameObject);
                }
            }

            // 创建新的手牌按钮
            for (int i = 0; i < hand.Count; i++)
            {
                int index = i;
                Card card = hand[i];

                Button cardButton = CreateButton(handPanel, $"Card_{i}", Vector2.zero, new Vector2(120, 110), $"{card.name}\n{card.description}");
                cardButton.name = $"Card_{i}";
                cardButton.onClick.RemoveAllListeners();
                cardButton.onClick.AddListener(() => PlayCard(index));
            }
        }

        // 动态创建所有UI（不用手动拖任何东西）
        void CreateAllUI()
        {
            // 创建Canvas
            GameObject canvasObj = new GameObject("Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();
            EnsureEventSystem();

            // 创建玩家血量文本
            playerHpText = CreateText(canvas.transform, "PlayerHp", new Vector2(20, -20), new Vector2(260, 32), "玩家生命：20/20", TextAnchor.MiddleLeft, new Vector2(0, 1), new Vector2(0, 1));

            // 创建敌人血量文本
            enemyHpText = CreateText(canvas.transform, "EnemyHp", new Vector2(-20, -20), new Vector2(260, 32), "敌人生命：15/15", TextAnchor.MiddleRight, new Vector2(1, 1), new Vector2(1, 1));

            // 创建玩家buff文本
            playerBuffText = CreateText(canvas.transform, "PlayerBuff", new Vector2(20, -58), new Vector2(360, 32), "玩家buff：无", TextAnchor.MiddleLeft, new Vector2(0, 1), new Vector2(0, 1));

            // 创建敌人buff文本
            enemyBuffText = CreateText(canvas.transform, "EnemyBuff", new Vector2(-20, -58), new Vector2(360, 32), "敌人buff：无", TextAnchor.MiddleRight, new Vector2(1, 1), new Vector2(1, 1));

            // 创建游戏信息文本
            gameInfoText = CreateText(canvas.transform, "GameInfo", new Vector2(0, -115), new Vector2(560, 60), "点击开始按钮开始游戏", TextAnchor.MiddleCenter, new Vector2(0.5f, 1), new Vector2(0.5f, 1));
            gameInfoText.fontSize = 24;

            // 创建手牌面板
            GameObject handPanelObj = new GameObject("HandPanel", typeof(RectTransform));
            handPanelObj.transform.SetParent(canvas.transform, false);
            handPanel = handPanelObj.GetComponent<RectTransform>();
            SetRectTransform(handPanel, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 24), new Vector2(900, 140));

            // 创建水平布局组
            HorizontalLayoutGroup layout = handPanelObj.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 12;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.padding = new RectOffset(12, 12, 8, 8);

            // 创建结束回合按钮
            endTurnButton = CreateButton(canvas.transform, "EndTurnButton", new Vector2(-24, 24), new Vector2(140, 44), "结束回合", TextAnchor.MiddleCenter, new Vector2(1, 0), new Vector2(1, 0));
            endTurnButton.onClick.AddListener(EndPlayerTurn);

            // 创建开始按钮
            startButton = CreateButton(canvas.transform, "StartButton", new Vector2(0, -20), new Vector2(140, 48), "开始游戏", TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            startButton.onClick.AddListener(() =>
            {
                InitGame();
                startButton.gameObject.SetActive(false);
                StartPlayerTurn();
            });
        }

        // 辅助函数：创建文本
        Text CreateText(Transform parent, string name, Vector2 pos, Vector2 size, string text)
        {
            return CreateText(parent, name, pos, size, text, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        }

        Text CreateText(Transform parent, string name, Vector2 pos, Vector2 size, string text, TextAnchor alignment, Vector2 anchor, Vector2 pivot)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);

            Text t = obj.AddComponent<Text>();
            t.text = text;
            t.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            t.color = Color.black;
            t.alignment = alignment;
            t.raycastTarget = false;
            SetRectTransform(obj.GetComponent<RectTransform>(), anchor, anchor, pivot, pos, size);

            return t;
        }

        // 辅助函数：创建按钮
        Button CreateButton(Transform parent, string name, Vector2 pos, Vector2 size, string text)
        {
            return CreateButton(parent, name, pos, size, text, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        }

        Button CreateButton(Transform parent, string name, Vector2 pos, Vector2 size, string text, TextAnchor alignment, Vector2 anchor, Vector2 pivot)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);

            Image img = obj.AddComponent<Image>();
            img.color = Color.white;

            Button b = obj.AddComponent<Button>();
            b.targetGraphic = img;

            Text t = CreateText(b.transform, "Text", Vector2.zero, size, text);
            t.color = Color.black;
            t.alignment = alignment;
            SetRectTransform(obj.GetComponent<RectTransform>(), anchor, anchor, pivot, pos, size);

            LayoutElement layoutElement = obj.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = size.x;
            layoutElement.preferredHeight = size.y;

            return b;
        }

        void EnsureEventSystem()
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                GameObject eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<EventSystem>();
                eventSystemObj.AddComponent<StandaloneInputModule>();
                return;
            }

            if (eventSystem.GetComponent<BaseInputModule>() == null)
            {
                eventSystem.gameObject.AddComponent<StandaloneInputModule>();
            }
        }

        void SetRectTransform(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 size)
        {
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.pivot = pivot;
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = size;
        }
    }
}

