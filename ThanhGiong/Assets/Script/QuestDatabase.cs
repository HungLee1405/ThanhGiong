using System.Collections.Generic;
using UnityEngine;

public class QuestDatabase : MonoBehaviour
{
    [Header("Item Rewards")]
    public ItemData axeItem;
    public ItemData pickaxeItem;

    // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    //  VOICE CLIPS â€“ GIÃ€ LÃ€NG (village_elder)  â€” 29 clips, 1 clip/dÃ²ng
    //  NgÃ y 1 (3 dÃ²ng) : Voice1GL  â€¦ Voice3GL
    //  NgÃ y 2 (4 dÃ²ng) : Voice4GL  â€¦ Voice7GL
    //  NgÃ y 3 (4 dÃ²ng) : Voice8GL  â€¦ Voice11GL
    //  NgÃ y 4 (5 dÃ²ng) : Voice12GL â€¦ Voice16GL
    //  NgÃ y 5 (6 dÃ²ng) : Voice17GL â€¦ Voice22GL
    //  NgÃ y 6 (5 dÃ²ng) : Voice23GL â€¦ Voice27GL
    //  NgÃ y 7 (2 dÃ²ng) : Voice28GL â€¦ Voice29GL
    // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    [Header("Voice â€“ GiÃ  LÃ ng (GL) â€” 29 clips")]
    public AudioClip voice1GL;
    public AudioClip voice2GL;
    public AudioClip voice3GL;
    public AudioClip voice4GL;
    public AudioClip voice5GL;
    public AudioClip voice6GL;
    public AudioClip voice7GL;
    public AudioClip voice8GL;
    public AudioClip voice9GL;
    public AudioClip voice10GL;
    public AudioClip voice11GL;
    public AudioClip voice12GL;
    public AudioClip voice13GL;
    public AudioClip voice14GL;
    public AudioClip voice15GL;
    public AudioClip voice16GL;
    public AudioClip voice17GL;
    public AudioClip voice18GL;
    public AudioClip voice19GL;
    public AudioClip voice20GL;
    public AudioClip voice21GL;
    public AudioClip voice22GL;
    public AudioClip voice23GL;
    public AudioClip voice24GL;
    public AudioClip voice25GL;
    public AudioClip voice26GL;
    public AudioClip voice27GL;
    public AudioClip voice28GL;
    public AudioClip voice29GL;

    // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    //  VOICE CLIPS â€“ Máº¸ GIÃ“NG (giong_mother)  â€” 10 clips
    //  Láº§n 1 (6 dÃ²ng) : Voice1MG  â€¦ Voice6MG
    //  Láº§n 2 (4 dÃ²ng) : Voice7MG  â€¦ Voice10MG
    // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    [Header("Voice â€“ Máº¹ GiÃ³ng (MG) â€” 10 clips")]
    public AudioClip voice1MG;
    public AudioClip voice2MG;
    public AudioClip voice3MG;
    public AudioClip voice4MG;
    public AudioClip voice5MG;
    public AudioClip voice6MG;
    public AudioClip voice7MG;
    public AudioClip voice8MG;
    public AudioClip voice9MG;
    public AudioClip voice10MG;

    // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    //  VOICE CLIPS â€“ BÃC BA (bac_ba)  â€” 7 clips
    //  Láº§n 1 (4 dÃ²ng) : Voice1Bacba â€¦ Voice4Bacba
    //  Láº§n 2 (3 dÃ²ng) : Voice5Bacba â€¦ Voice7Bacba
    // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    [Header("Voice â€“ BÃ¡c Ba (Bacba) â€” 7 clips")]
    public AudioClip voice1Bacba;
    public AudioClip voice2Bacba;
    public AudioClip voice3Bacba;
    public AudioClip voice4Bacba;
    public AudioClip voice5Bacba;
    public AudioClip voice6Bacba;
    public AudioClip voice7Bacba;

    // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    //  VOICE CLIPS â€“ THá»¢ RÃˆN / BLACKSMITH  â€” 11 clips
    //  NgÃ y 4 (4 dÃ²ng) : Voice1ThoRen  â€¦ Voice4ThoRen
    //  NgÃ y 7 (7 dÃ²ng) : Voice5ThoRen  â€¦ Voice11ThoRen
    // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    [Header("Voice â€“ Thá»£ RÃ¨n (ThoRen) â€” 11 clips")]
    public AudioClip voice1ThoRen;
    public AudioClip voice2ThoRen;
    public AudioClip voice3ThoRen;
    public AudioClip voice4ThoRen;
    public AudioClip voice5ThoRen;
    public AudioClip voice6ThoRen;
    public AudioClip voice7ThoRen;
    public AudioClip voice8ThoRen;
    public AudioClip voice9ThoRen;
    public AudioClip voice10ThoRen;
    public AudioClip voice11ThoRen;

    public List<QuestStep> GetQuestStepsForDay(int day)
    {
        switch (day)
        {
            case 1: return GetDay1Steps();
            case 2: return GetDay2Steps();
            case 3: return GetDay3Steps();
            case 4: return GetDay4Steps();
            case 5: return GetDay5Steps();
            case 6: return GetDay6Steps();
            case 7: return GetDay7Steps();
            default: return new List<QuestStep>();
        }
    }

    // â”€â”€â”€ NGÃ€Y 1 â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    private List<QuestStep> GetDay1Steps()
    {
        return new List<QuestStep>
        {
            // GiÃ  LÃ ng â€“ ngÃ y 1 (3 dÃ²ng â†’ Voice1-3GL)
            new QuestStep
            {
                day = 1,
                questName = "Nhiá»‡m vá»¥ ngÃ y 1",
                questDescription = "Äáº¿n gáº·p GiÃ  LÃ ng.",
                stepType = QuestStepType.TalkToNPC,
                targetNPCId = "village_elder",
                requiredAmount = 1,
                dialogueLines = new string[]
                {
                    "Váº­y lÃ  cáº­u lÃ  ngÆ°á»i Ä‘Æ°á»£c phÃ¡i tá»›i Ã .",
                    "Ta lÃ  GiÃ  LÃ ng PhÃ¹ Äá»•ng, tháº­t tá»‘t khi thiáº¿u niÃªn tráº» váº«n cÃ²n hÄƒng hÃ¡i.",
                    "HÃ£y Ä‘i gáº·p máº¹ GiÃ³ng Ä‘á»ƒ biáº¿t mÃ¬nh cáº§n lÃ m gÃ¬ nhÃ©."
                },
                voiceClips = new AudioClip[] { voice1GL, voice2GL, voice3GL }
            },

            // Máº¹ GiÃ³ng â€“ láº§n 1 (6 dÃ²ng â†’ Voice1-6MG)
            new QuestStep
            {
                day = 1,
                questName = "Nhiá»‡m vá»¥ ngÃ y 1",
                questDescription = "NÃ³i chuyá»‡n vá»›i máº¹ GiÃ³ng.",
                stepType = QuestStepType.TalkToNPC,
                targetNPCId = "giong_mother",
                requiredAmount = 1,
                dialogueLines = new string[]
                {
                    "Váº­y cáº­u lÃ  ngÆ°á»i mÃ  GiÃ  LÃ ng nÃ³i tá»›i.",
                    "TÃ´i lÃ  máº¹ GiÃ³ng. Hiá»‡n giá» con trai tÃ´i Ä‘ang Äƒn ráº¥t nhiá»u.",
                    "TÃ´i náº¥u khÃ´ng xuá»ƒ dÃ¹ má»i ngÆ°á»i cÃ³ gÃ³p gáº¡o gÃ³p sá»©c.",
                    "HÃ£y giÃºp tÃ´i náº¥u Äƒn nhÃ©.",
                    "Cáº­u cÃ³ thá»ƒ láº¥y nÆ°á»›c tá»« giáº¿ng, gáº¡o tá»« kho thÃ³c vÃ  Ä‘áº¿n ná»“i á»Ÿ giá»¯a lÃ ng Ä‘á»ƒ náº¥u.",
                    "Sau khi náº¥u xong hÃ£y Ä‘áº¿n gáº·p tÃ´i."
                },
                voiceClips = new AudioClip[] { voice1MG, voice2MG, voice3MG, voice4MG, voice5MG, voice6MG }
            },

            new QuestStep
            {
                day = 1,
                questName = "Nhiá»‡m vá»¥ ngÃ y 1",
                questDescription = "Láº¥y nÆ°á»›c tá»« giáº¿ng.",
                stepType = QuestStepType.CollectWater,
                targetItemId = "water",
                requiredAmount = 1
            },

            new QuestStep
            {
                day = 1,
                questName = "Nhiá»‡m vá»¥ ngÃ y 1",
                questDescription = "Láº¥y gáº¡o tá»« kho thÃ³c.",
                stepType = QuestStepType.CollectRice,
                targetItemId = "rice",
                requiredAmount = 1
            },

            new QuestStep
            {
                day = 1,
                questName = "Nhiá»‡m vá»¥ ngÃ y 1",
                questDescription = "Náº¥u cÆ¡m táº¡i ná»“i giá»¯a lÃ ng.",
                stepType = QuestStepType.CookRice,
                targetItemId = "cooked_rice",
                requiredAmount = 1
            },

            new QuestStep
            {
                day = 1,
                questName = "Nhiá»‡m vá»¥ ngÃ y 1",
                questDescription = "ÄÆ°a cÆ¡m cho máº¹ GiÃ³ng.",
                stepType = QuestStepType.FeedGiong,
                targetItemId = "cooked_rice",
                requiredAmount = 1
            },

            // Máº¹ GiÃ³ng â€“ láº§n 2 (4 dÃ²ng â†’ Voice7-10MG)
            new QuestStep
            {
                day = 1,
                questName = "Nhiá»‡m vá»¥ ngÃ y 1",
                questDescription = "NÃ³i chuyá»‡n láº¡i vá»›i máº¹ GiÃ³ng.",
                stepType = QuestStepType.TalkToNPC,
                targetNPCId = "giong_mother",
                requiredAmount = 1,
                dialogueLines = new string[]
                {
                    "Tuyá»‡t vá»i, cáº­u lÃ m tá»‘t láº¯m.",
                    "Tuy nhiÃªn nhiÃªu Ä‘Ã¢y lÃ  chÆ°a Ä‘á»§, con tÃ´i giá» Ä‘Ã¢y Ä‘ang Äƒn ráº¥t nhiá»u.",
                    "HÃ£y náº¥u vÃ  Ä‘Æ°a cÆ¡m cho tÃ´i Ä‘á»ƒ tÃ´i Ä‘Æ°a cho nÃ³ Äƒn.",
                    "BÃªn pháº£i mÃ n hÃ¬nh lÃ  thanh Ä‘Ã³i, hÃ£y giÃºp tÃ´i giá»¯ nÃ³ trÃªn 80% khi háº¿t ngÃ y."
                },
                voiceClips = new AudioClip[] { voice7MG, voice8MG, voice9MG, voice10MG }
            },

            new QuestStep
            {
                day = 1,
                questName = "Nhiá»‡m vá»¥ ngÃ y 1",
                questDescription = "Náº¥u cÆ¡m vÃ  giá»¯ cho thanh Ä‘Ã³i cá»§a GiÃ³ng trÃªn 80% khi ngÃ y káº¿t thÃºc.",
                stepType = QuestStepType.SurviveUntilDayEnd,
                requiredAmount = 1
            }
        };
    }

    // â”€â”€â”€ NGÃ€Y 2 â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    private List<QuestStep> GetDay2Steps()
    {
        return new List<QuestStep>
        {
            // GiÃ  LÃ ng â€“ ngÃ y 2 (4 dÃ²ng â†’ Voice4-7GL)
            new QuestStep
            {
                day = 2,
                questName = "Nhiá»‡m vá»¥ ngÃ y 2",
                questDescription = "NÃ³i chuyá»‡n vá»›i GiÃ  LÃ ng.",
                stepType = QuestStepType.TalkToNPC,
                targetNPCId = "village_elder",
                requiredAmount = 1,
                dialogueLines = new string[]
                {
                    "HÃ´m qua cáº­u lÃ m tá»‘t láº¯m.",
                    "Ta tin tÆ°á»Ÿng vÃ o cáº­u hÃ´m nay.",
                    "Tuy nhiÃªn GiÃ³ng Ä‘ang ngÃ y cÃ ng lá»›n lÃªn, sá»©c Äƒn cÅ©ng lá»›n hÆ¡n.",
                    "Thanh Ä‘Ã³i sáº½ cÃ ng ngÃ y cÃ ng tá»¥t nhanh hÆ¡n. HÃ£y cáº©n tháº­n."
                },
                voiceClips = new AudioClip[] { voice4GL, voice5GL, voice6GL, voice7GL }
            },

            new QuestStep
            {
                day = 2,
                questName = "Nhiá»‡m vá»¥ ngÃ y 2",
                questDescription = "Náº¥u cÆ¡m vÃ  giá»¯ cho thanh Ä‘Ã³i cá»§a GiÃ³ng trÃªn 80% khi ngÃ y káº¿t thÃºc.",
                stepType = QuestStepType.SurviveUntilDayEnd,
                requiredAmount = 1
            }
        };
    }

    // â”€â”€â”€ NGÃ€Y 3 â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    private List<QuestStep> GetDay3Steps()
    {
        return new List<QuestStep>
        {
            // GiÃ  LÃ ng â€“ ngÃ y 3 (4 dÃ²ng â†’ Voice8-11GL)
            new QuestStep
            {
                day = 3,
                questName = "Nhiá»‡m vá»¥ ngÃ y 3",
                questDescription = "NÃ³i chuyá»‡n vá»›i GiÃ  LÃ ng.",
                stepType = QuestStepType.TalkToNPC,
                targetNPCId = "village_elder",
                requiredAmount = 1,
                dialogueLines = new string[]
                {
                    "HÃ´m qua cáº­u lÃ m tá»‘t láº¯m.",
                    "Ta tin tÆ°á»Ÿng vÃ o cáº­u hÃ´m nay.",
                    "Ã€ mÃ , bÃ¡c Ba cÃ³ váº» gáº·p khÃ³ khÄƒn.",
                    "Náº¿u Ä‘Æ°á»£c hÃ£y ghÃ© qua giÃºp Ã´ng áº¥y má»™t tay nhÃ©."
                },
                voiceClips = new AudioClip[] { voice8GL, voice9GL, voice10GL, voice11GL }
            },

            new QuestStep
            {
                day = 3,
                questName = "Nhiá»‡m vá»¥ ngÃ y 3",
                questDescription = "Giá»¯ thanh Ä‘Ã³i cá»§a GiÃ³ng trÃªn 80% vÃ  hoÃ n thÃ nh viá»‡c giÃºp bÃ¡c Ba báº¯t gÃ .",
                stepType = QuestStepType.SurviveUntilDayEnd,
                requiredAmount = 1,
                isSideQuest = false
            },

            // BÃ¡c Ba â€“ láº§n 1 (4 dÃ²ng â†’ Voice1-4Bacba)
            new QuestStep
            {
                day = 3,
                questName = "Nhiá»‡m vá»¥ phá»¥",
                questDescription = "Äáº¿n gáº·p bÃ¡c Ba.",
                stepType = QuestStepType.TalkToNPC,
                targetNPCId = "bac_ba",
                requiredAmount = 1,
                isSideQuest = true,
                unlockAtMainStepIndex = 1,
                dialogueLines = new string[]
                {
                    "ChÃ o cáº­u tráº», ta lÃ  bÃ¡c Ba.",
                    "LÅ© gÃ  cá»§a ta nghá»‹ch ngá»£m quÃ¡, Ä‘Ã£ xá»•ng chuá»“ng cháº¡y Ä‘i kháº¯p nÆ¡i rá»“i.",
                    "Cáº­u giÃºp ta báº¯t 3 con gÃ  bá» láº¡i vÃ o chuá»“ng Ä‘Æ°á»£c khÃ´ng?",
                    "Sau Ä‘Ã³ ta sáº½ truyá»n láº¡i bÃ­ quyáº¿t náº¥u mÃ³n CÆ¡m GÃ  Ä‘á»ƒ giÃºp GiÃ³ng mau lá»›n!"
                },
                voiceClips = new AudioClip[] { voice1Bacba, voice2Bacba, voice3Bacba, voice4Bacba }
            },

            new QuestStep
            {
                day = 3,
                questName = "Nhiá»‡m vá»¥ phá»¥",
                questDescription = "Báº¯t gÃ  vÃ  Ä‘Æ°a vá» chuá»“ng.",
                stepType = QuestStepType.CatchChicken,
                targetItemId = "chick",
                requiredAmount = 3,
                isSideQuest = true,
                unlockAtMainStepIndex = 1
            },

            // BÃ¡c Ba â€“ láº§n 2 (3 dÃ²ng â†’ Voice5-7Bacba)
            new QuestStep
            {
                day = 3,
                questName = "Nhiá»‡m vá»¥ phá»¥",
                questDescription = "NÃ³i chuyá»‡n láº¡i vá»›i bÃ¡c Ba.",
                stepType = QuestStepType.TalkToNPC,
                targetNPCId = "bac_ba",
                requiredAmount = 1,
                isSideQuest = true,
                unlockAtMainStepIndex = 1,
                dialogueLines = new string[]
                {
                    "Ã”i cáº­u lÃ m tá»‘t quÃ¡, cáº£m Æ¡n cáº­u nhiá»u nhÃ©!",
                    "LÅ© gÃ  giá» Ä‘Ã£ á»Ÿ yÃªn trong chuá»“ng rá»“i.",
                    "Ta Ä‘Ã£ chuáº©n bá»‹ cÃ´ng thá»©c náº¥u CÆ¡m GÃ  cho cáº­u rá»“i Ä‘áº¥y, hÃ£y dÃ¹ng nÃ³ Ä‘á»ƒ cho GiÃ³ng Äƒn nhÃ©!"
                },
                voiceClips = new AudioClip[] { voice5Bacba, voice6Bacba, voice7Bacba }
            }
        };
    }

    // â”€â”€â”€ NGÃ€Y 4 â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    private List<QuestStep> GetDay4Steps()
    {
        return new List<QuestStep>
        {
            // GiÃ  LÃ ng â€“ ngÃ y 4 (5 dÃ²ng â†’ Voice12-16GL)
            new QuestStep
            {
                day = 4,
                questName = "Nhiá»‡m vá»¥ ngÃ y 4",
                questDescription = "NÃ³i chuyá»‡n vá»›i GiÃ  LÃ ng.",
                stepType = QuestStepType.TalkToNPC,
                targetNPCId = "village_elder",
                requiredAmount = 1,
                dialogueLines = new string[]
                {
                    "HÃ´m nay nhiá»‡m vá»¥ sáº½ khÃ¡c hÃ´m qua.",
                    "MÃ  giáº·c Ã‚n Ä‘Ã£ Ä‘áº¿n ráº¥t gáº§n rá»“i.",
                    "ChÃºng ta pháº£i chuáº©n bá»‹ vÅ© khÃ­ cho GiÃ³ng.",
                    "Cáº­u hÃ£y Ä‘áº¿n gáº·p bÃ¡c thá»£ rÃ¨n á»Ÿ rÃ¬a lÃ ng.",
                    "BÃ¡c áº¥y Ä‘ang cáº§n quáº·ng sáº¯t Ä‘á»ƒ chuáº©n bá»‹ Ä‘Ãºc ngá»±a vÃ  roi sáº¯t Ä‘áº¥y."
                },
                voiceClips = new AudioClip[] { voice12GL, voice13GL, voice14GL, voice15GL, voice16GL }
            },

            // Thá»£ RÃ¨n â€“ ngÃ y 4 (4 dÃ²ng â†’ Voice1-4ThoRen)
            new QuestStep
            {
                day = 4,
                questName = "Nhiá»‡m vá»¥ phá»¥",
                questDescription = "Gáº·p BÃ¡c Thá»£ RÃ¨n.",
                stepType = QuestStepType.TalkToNPC,
                targetNPCId = "blacksmith",
                isSideQuest = true,
                unlockAtMainStepIndex = 1,
                requiredAmount = 1,
                rewardItem = pickaxeItem,
                rewardAmount = 1,
                rewardTiming = RewardTiming.TalkToNPC,
                rewardMessage = "Nháº­n Ä‘Æ°á»£c Cuá»‘c Chim!",
                requireInventorySpace = true,
                dialogueLines = new string[]
                {
                    "Cáº­u áº¯t háº³n lÃ  ngÆ°á»i mÃ  GiÃ  LÃ ng nháº¯c Ä‘áº¿n.",
                    "Cá»§i lá»­a Ä‘Ã£ sáºµn sÃ ng nhÆ°ng ta Ä‘ang thiáº¿u quáº·ng sáº¯t tráº§m trá»ng.",
                    "Cáº­u hÃ£y cáº§m láº¥y cÃ¢y cuá»‘c chim nÃ y.",
                    "Ra má» Ä‘Ã¡ phÃ­a sau lÃ ng Ä‘Ã o má»™t Ã­t Quáº·ng Sáº¯t mang vá» kho giÃºp ta nhÃ©!"
                },
                voiceClips = new AudioClip[] { voice1ThoRen, voice2ThoRen, voice3ThoRen, voice4ThoRen }
            },

            new QuestStep
            {
                day = 4,
                questName = "Nhiá»‡m vá»¥ phá»¥",
                questDescription = "Khai thÃ¡c 2 quáº·ng sáº¯t vÃ  mang vá» kho",
                stepType = QuestStepType.CollectIron,
                targetItemId = "iron_ore",
                requiredAmount = 2,
                isSideQuest = true,
                unlockAtMainStepIndex = 1
            },

            new QuestStep
            {
                day = 4,
                questName = "Nhiá»‡m vá»¥ ngÃ y 4",
                questDescription = "Giá»¯ thanh Ä‘Ã³i cá»§a GiÃ³ng trÃªn 80% vÃ  há»— trá»£ thá»£ rÃ¨n chuáº©n bá»‹ quáº·ng sáº¯t.",
                stepType = QuestStepType.SurviveUntilDayEnd,
                requiredAmount = 1
            }
        };
    }

    // â”€â”€â”€ NGÃ€Y 5 â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    private List<QuestStep> GetDay5Steps()
    {
        return new List<QuestStep>
        {
            // GiÃ  LÃ ng â€“ ngÃ y 5 (6 dÃ²ng â†’ Voice17-22GL)
            new QuestStep
            {
                day = 5,
                questName = "Nhiá»‡m vá»¥ ngÃ y 5",
                questDescription = "NÃ³i chuyá»‡n vá»›i GiÃ  LÃ ng.",
                stepType = QuestStepType.TalkToNPC,
                targetNPCId = "village_elder",
                requiredAmount = 1,
                rewardItem = axeItem,
                rewardAmount = 1,
                rewardTiming = RewardTiming.TalkToNPC,
                rewardMessage = "Nháº­n Ä‘Æ°á»£c RÃ¬u!",
                requireInventorySpace = true,
                dialogueLines = new string[]
                {
                    "TÃ¬nh hÃ¬nh nguy cáº¥p rá»“i cáº­u tráº» Æ¡i!",
                    "Giáº·c Ã‚n Ä‘Ã£ Ã¡p sÃ¡t biÃªn thÃ¹y.",
                    "LÃ ng ta cáº§n gáº¥p má»™t lÆ°á»£ng tre lá»›n Ä‘á»ƒ lÃ m cá»c phÃ²ng thá»§.",
                    "Khu vá»±c Rá»«ng Tre á»Ÿ phÃ­a ÄÃ´ng, cáº­u hÃ£y mang rÃ¬u ra Ä‘Ã³ thu hoáº¡ch nhÃ©.",
                    "Ã€, sáºµn cÃ³ á»‘ng tre tÆ°Æ¡i, cáº­u cÃ³ thá»ƒ náº¥u mÃ³n CÆ¡m Lam á»ng Tre cho GiÃ³ng.",
                    "Tháº±ng bÃ© giá» Ä‘Ã£ lá»›n báº±ng ngÃ´i nhÃ , sá»©c Äƒn kinh khá»§ng láº¯m."
                },
                voiceClips = new AudioClip[] { voice16GL, voice17GL, voice18GL, voice19GL, voice21GL, voice22GL }
            },

            new QuestStep
            {
                day = 5,
                questName = "Nhiá»‡m vá»¥ phá»¥",
                questDescription = "Cháº·t 5 bÃ³ tre mang vá» kho.",
                stepType = QuestStepType.CollectBamboo,
                targetItemId = "bamboo",
                requiredAmount = 5,
                isSideQuest = true,
                unlockAtMainStepIndex = 1
            },

            new QuestStep
            {
                day = 5,
                questName = "Nhiá»‡m vá»¥ ngÃ y 5",
                questDescription = "Náº¥u Äƒn vÃ  giá»¯ cho thanh Ä‘Ã³i cá»§a GiÃ³ng trÃªn 80% khi ngÃ y káº¿t thÃºc.",
                stepType = QuestStepType.SurviveUntilDayEnd,
                requiredAmount = 1
            }
        };
    }

    // â”€â”€â”€ NGÃ€Y 6 â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    private List<QuestStep> GetDay6Steps()
    {
        return new List<QuestStep>
        {
            // GiÃ  LÃ ng â€“ ngÃ y 6 (5 dÃ²ng â†’ Voice23-27GL)
            new QuestStep
            {
                day = 6,
                questName = "Nhiá»‡m vá»¥ ngÃ y 6",
                questDescription = "NÃ³i chuyá»‡n vá»›i GiÃ  LÃ ng.",
                stepType = QuestStepType.TalkToNPC,
                targetNPCId = "village_elder",
                requiredAmount = 1,
                dialogueLines = new string[]
                {
                    "NgÃ y mai lÃ  thá»£ rÃ¨n báº¯t Ä‘áº§u Ä‘Ãºc vÅ© khÃ­ tháº§n tá»‘c rá»“i.",
                    "HÃ´m nay lÃ  ngÃ y cao Ä‘iá»ƒm Ä‘á»ƒ tÃ­ch lÅ©y tÃ i nguyÃªn.",
                    "ChÃºng ta pháº£i dá»‘c toÃ n lá»±c!",
                    "HÃ£y vá»«a cho GiÃ³ng Äƒn, vá»«a váº­n chuyá»ƒn tháº­t nhiá»u Sáº¯t vÃ  Tre vÃ o kho dá»± trá»¯.",
                    "Sá»©c Äƒn cá»§a GiÃ³ng hÃ´m nay Ä‘Ã£ Ä‘áº¡t Ä‘á»‰nh, cáº­u pháº£i hoáº¡t Ä‘á»™ng háº¿t cÃ´ng suáº¥t Ä‘áº¥y!"
                },
                voiceClips = new AudioClip[] { voice23GL, voice24GL, voice25GL, voice26GL, voice27GL }
            },

            new QuestStep
            {
                day = 6,
                questName = "Nhiá»‡m vá»¥ ngÃ y 6",
                questDescription = "Giá»¯ thanh Ä‘Ã³i trÃªn 80%, tÃ­ch trá»¯ Ã­t nháº¥t 5 quáº·ng sáº¯t vÃ  10 bÃ³ tre.",
                stepType = QuestStepType.SurviveUntilDayEnd,
                requiredAmount = 1,
                storageRequirements = new List<QuestRequirement>
                {
                    new QuestRequirement { targetItemId = "iron_ore", requiredAmount = 5 },
                    new QuestRequirement { targetItemId = "bamboo", requiredAmount = 10 }
                }
            }
        };
    }

    // â”€â”€â”€ NGÃ€Y 7 â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    private List<QuestStep> GetDay7Steps()
    {
        return new List<QuestStep>
        {
            // GiÃ  LÃ ng â€“ ngÃ y 7 (2 dÃ²ng â†’ Voice28-29GL)
            new QuestStep
            {
                day = 7,
                questName = "Nhiá»‡m vá»¥ ngÃ y 7",
                questDescription = "NÃ³i chuyá»‡n vá»›i GiÃ  LÃ ng.",
                stepType = QuestStepType.TalkToNPC,
                targetNPCId = "village_elder",
                requiredAmount = 1,
                dialogueLines = new string[]
                {
                    "HÃ´m nay lÃ  ngÃ y cuá»‘i cÃ¹ng rá»“i.",
                    "HÃ£y gáº·p bÃ¡c thá»£ rÃ¨n Ä‘á»ƒ biáº¿t mÃ¬nh cáº§n lÃ m gÃ¬ nhÃ©."
                },
                voiceClips = new AudioClip[] { voice23GL, voice29GL }
            },

            // Thá»£ RÃ¨n â€“ ngÃ y 7 (7 dÃ²ng â†’ Voice5-11ThoRen)
            new QuestStep
            {
                day = 7,
                questName = "Nhiá»‡m vá»¥ ngÃ y 7",
                questDescription = "NÃ³i chuyá»‡n vá»›i BÃ¡c Thá»£ RÃ¨n.",
                stepType = QuestStepType.TalkToNPC,
                targetNPCId = "blacksmith",
                requiredAmount = 1,
                dialogueLines = new string[]
                {
                    "Giáº·c Ä‘Ã£ Ä‘áº¿n Ä‘áº§u lÃ ng rá»“i!",
                    "Thá»i kháº¯c quyáº¿t Ä‘á»‹nh Ä‘Ã£ Ä‘áº¿n!",
                    "Tuy nhiÃªn chÃºng ta thiáº¿u quÃ¡ nhiá»u sáº¯t.",
                    "Cáº­u tráº» hÃ£y giÃºp ta má»™t tay.",
                    "HÃ£y Ä‘Ã o vÃ  mang 10 viÃªn sáº¯t vÃ o kho.",
                    "Äá»ƒ ta Ä‘Ãºc ngá»±a sáº¯t, roi sáº¯t!",
                    "Song song Ä‘Ã³, váº«n pháº£i Ä‘Æ°a cÆ¡m cho GiÃ³ng Äƒn no Ä‘á»ƒ chuáº©n bá»‹ xuáº¥t quÃ¢n!"
                },
                voiceClips = new AudioClip[] { voice5ThoRen, voice6ThoRen, voice7ThoRen, voice8ThoRen, voice9ThoRen, voice10ThoRen, voice11ThoRen }
            },

            new QuestStep
            {
                day = 7,
                questName = "Nhiá»‡m vá»¥ ngÃ y 7",
                questDescription = "Giá»¯ thanh Ä‘Ã³i trÃªn 80% vÃ  tÃ­ch trá»¯ Ã­t nháº¥t 10 quáº·ng sáº¯t.",
                stepType = QuestStepType.SurviveUntilDayEnd,
                requiredAmount = 1,
                storageRequirements = new List<QuestRequirement>
                {
                    new QuestRequirement { targetItemId = "iron_ore", requiredAmount = 10 }
                }
            }
        };
    }
}
