using System.Collections.Generic;
using UnityEngine;

public class QuestDatabase : MonoBehaviour
{
    [Header("Item Rewards")]
    public ItemData axeItem;
    public ItemData pickaxeItem;

    // ─────────────────────────────────────────────────────────────────
    //  VOICE CLIPS – GIÀ LÀNG (village_elder)  — 29 clips, 1 clip/dòng
    //  Ngày 1 (3 dòng) : Voice1GL  … Voice3GL
    //  Ngày 2 (4 dòng) : Voice4GL  … Voice7GL
    //  Ngày 3 (4 dòng) : Voice8GL  … Voice11GL
    //  Ngày 4 (5 dòng) : Voice12GL … Voice16GL
    //  Ngày 5 (6 dòng) : Voice17GL … Voice22GL
    //  Ngày 6 (5 dòng) : Voice23GL … Voice27GL
    //  Ngày 7 (2 dòng) : Voice28GL … Voice29GL
    // ─────────────────────────────────────────────────────────────────
    [Header("Voice – Già Làng (GL) — 29 clips")]
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

    // ─────────────────────────────────────────────────────────────────
    //  VOICE CLIPS – MẸ GIÓNG (giong_mother)  — 10 clips
    //  Lần 1 (6 dòng) : Voice1MG  … Voice6MG
    //  Lần 2 (4 dòng) : Voice7MG  … Voice10MG
    // ─────────────────────────────────────────────────────────────────
    [Header("Voice – Mẹ Gióng (MG) — 10 clips")]
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

    // ─────────────────────────────────────────────────────────────────
    //  VOICE CLIPS – BÁC BA (bac_ba)  — 7 clips
    //  Lần 1 (4 dòng) : Voice1Bacba … Voice4Bacba
    //  Lần 2 (3 dòng) : Voice5Bacba … Voice7Bacba
    // ─────────────────────────────────────────────────────────────────
    [Header("Voice – Bác Ba (Bacba) — 7 clips")]
    public AudioClip voice1Bacba;
    public AudioClip voice2Bacba;
    public AudioClip voice3Bacba;
    public AudioClip voice4Bacba;
    public AudioClip voice5Bacba;
    public AudioClip voice6Bacba;
    public AudioClip voice7Bacba;

    // ─────────────────────────────────────────────────────────────────
    //  VOICE CLIPS – THỢ RÈN / BLACKSMITH  — 11 clips
    //  Ngày 4 (4 dòng) : Voice1ThoRen  … Voice4ThoRen
    //  Ngày 7 (7 dòng) : Voice5ThoRen  … Voice11ThoRen
    // ─────────────────────────────────────────────────────────────────
    [Header("Voice – Thợ Rèn (ThoRen) — 11 clips")]
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

    // ─── NGÀY 1 ────────────────────────────────────────────────────
    private List<QuestStep> GetDay1Steps()
    {
        return new List<QuestStep>
        {
            // Già Làng – ngày 1 (3 dòng → Voice1-3GL)
            new QuestStep
            {
                day = 1,
                questName = "Nhiệm vụ ngày 1",
                questDescription = "Đến gặp Già Làng.",
                stepType = QuestStepType.TalkToNPC,
                targetNPCId = "village_elder",
                requiredAmount = 1,
                dialogueLines = new string[]
                {
                    "Vậy là cậu là người được phái tới à.",
                    "Ta là Già Làng Phù Đổng, thật tốt khi thiếu niên trẻ vẫn còn hăng hái.",
                    "Hãy đi gặp mẹ Gióng để biết mình cần làm gì nhé."
                },
                voiceClips = new AudioClip[] { voice1GL, voice2GL, voice3GL }
            },

            // Mẹ Gióng – lần 1 (6 dòng → Voice1-6MG)
            new QuestStep
            {
                day = 1,
                questName = "Nhiệm vụ ngày 1",
                questDescription = "Nói chuyện với mẹ Gióng.",
                stepType = QuestStepType.TalkToNPC,
                targetNPCId = "giong_mother",
                requiredAmount = 1,
                dialogueLines = new string[]
                {
                    "Vậy cậu là người mà Già Làng nói tới.",
                    "Tôi là mẹ Gióng. Hiện giờ con trai tôi đang ăn rất nhiều.",
                    "Tôi nấu không xuể dù mọi người có góp gạo góp sức.",
                    "Hãy giúp tôi nấu ăn nhé.",
                    "Cậu có thể lấy nước từ giếng, gạo từ kho thóc và đến nồi ở giữa làng để nấu.",
                    "Sau khi nấu xong hãy đến gặp tôi."
                },
                voiceClips = new AudioClip[] { voice1MG, voice2MG, voice3MG, voice4MG, voice5MG, voice6MG }
            },

            new QuestStep
            {
                day = 1,
                questName = "Nhiệm vụ ngày 1",
                questDescription = "Lấy nước từ giếng.",
                stepType = QuestStepType.CollectWater,
                targetItemId = "water",
                requiredAmount = 1
            },

            new QuestStep
            {
                day = 1,
                questName = "Nhiệm vụ ngày 1",
                questDescription = "Lấy gạo từ kho thóc.",
                stepType = QuestStepType.CollectRice,
                targetItemId = "rice",
                requiredAmount = 1
            },

            new QuestStep
            {
                day = 1,
                questName = "Nhiệm vụ ngày 1",
                questDescription = "Nấu cơm tại nồi giữa làng.",
                stepType = QuestStepType.CookRice,
                targetItemId = "cooked_rice",
                requiredAmount = 1
            },

            new QuestStep
            {
                day = 1,
                questName = "Nhiệm vụ ngày 1",
                questDescription = "Đưa cơm cho mẹ Gióng.",
                stepType = QuestStepType.FeedGiong,
                targetItemId = "cooked_rice",
                requiredAmount = 1
            },

            // Mẹ Gióng – lần 2 (4 dòng → Voice7-10MG)
            new QuestStep
            {
                day = 1,
                questName = "Nhiệm vụ ngày 1",
                questDescription = "Nói chuyện lại với mẹ Gióng.",
                stepType = QuestStepType.TalkToNPC,
                targetNPCId = "giong_mother",
                requiredAmount = 1,
                dialogueLines = new string[]
                {
                    "Tuyệt vời, cậu làm tốt lắm.",
                    "Tuy nhiên nhiêu đây là chưa đủ, con tôi giờ đây đang ăn rất nhiều.",
                    "Hãy nấu và đưa cơm cho tôi để tôi đưa cho nó ăn.",
                    "Bên phải màn hình là thanh đói, hãy giúp tôi giữ nó trên 80% khi hết ngày."
                },
                voiceClips = new AudioClip[] { voice7MG, voice8MG, voice9MG, voice10MG }
            },

            new QuestStep
            {
                day = 1,
                questName = "Nhiệm vụ ngày 1",
                questDescription = "Nấu cơm và giữ cho thanh đói của Gióng trên 80% khi ngày kết thúc.",
                stepType = QuestStepType.SurviveUntilDayEnd,
                requiredAmount = 1
            }
        };
    }

    // ─── NGÀY 2 ────────────────────────────────────────────────────
    private List<QuestStep> GetDay2Steps()
    {
        return new List<QuestStep>
        {
            // Già Làng – ngày 2 (4 dòng → Voice4-7GL)
            new QuestStep
            {
                day = 2,
                questName = "Nhiệm vụ ngày 2",
                questDescription = "Nói chuyện với Già Làng.",
                stepType = QuestStepType.TalkToNPC,
                targetNPCId = "village_elder",
                requiredAmount = 1,
                dialogueLines = new string[]
                {
                    "Hôm qua cậu làm tốt lắm.",
                    "Ta tin tưởng vào cậu hôm nay.",
                    "Tuy nhiên Gióng đang ngày càng lớn lên, sức ăn cũng lớn hơn.",
                    "Thanh đói sẽ càng ngày càng tụt nhanh hơn. Hãy cẩn thận."
                },
                voiceClips = new AudioClip[] { voice4GL, voice5GL, voice6GL, voice7GL }
            },

            new QuestStep
            {
                day = 2,
                questName = "Nhiệm vụ ngày 2",
                questDescription = "Nấu cơm và giữ cho thanh đói của Gióng trên 80% khi ngày kết thúc.",
                stepType = QuestStepType.SurviveUntilDayEnd,
                requiredAmount = 1
            }
        };
    }

    // ─── NGÀY 3 ────────────────────────────────────────────────────
    private List<QuestStep> GetDay3Steps()
    {
        return new List<QuestStep>
        {
            // Già Làng – ngày 3 (4 dòng → Voice8-11GL)
            new QuestStep
            {
                day = 3,
                questName = "Nhiệm vụ ngày 3",
                questDescription = "Nói chuyện với Già Làng.",
                stepType = QuestStepType.TalkToNPC,
                targetNPCId = "village_elder",
                requiredAmount = 1,
                dialogueLines = new string[]
                {
                    "Hôm qua cậu làm tốt lắm.",
                    "Ta tin tưởng vào cậu hôm nay.",
                    "À mà, bác Ba có vẻ gặp khó khăn.",
                    "Nếu được hãy ghé qua giúp ông ấy một tay nhé."
                },
                voiceClips = new AudioClip[] { voice4GL, voice5GL, voice8GL, voice9GL }
            },

            new QuestStep
            {
                day = 3,
                questName = "Nhiệm vụ ngày 3",
                questDescription = "Nấu cơm và giữ cho thanh đói của Gióng trên 80% khi ngày kết thúc.",
                stepType = QuestStepType.SurviveUntilDayEnd,
                requiredAmount = 1,
                isSideQuest = false
            },

            // Bác Ba – lần 1 (4 dòng → Voice1-4Bacba)
            new QuestStep
            {
                day = 3,
                questName = "Nhiệm vụ phụ",
                questDescription = "Đến gặp bác Ba.",
                stepType = QuestStepType.TalkToNPC,
                targetNPCId = "bac_ba",
                requiredAmount = 1,
                isSideQuest = true,
                unlockAtMainStepIndex = 1,
                dialogueLines = new string[]
                {
                    "Chào cậu trẻ, ta là bác Ba.",
                    "Lũ gà của ta nghịch ngợm quá, đã xổng chuồng chạy đi khắp nơi rồi.",
                    "Cậu giúp ta bắt 3 con gà bỏ lại vào chuồng được không?",
                    "Sau đó ta sẽ truyền lại bí quyết nấu món Cơm Gà để giúp Gióng mau lớn!"
                },
                voiceClips = new AudioClip[] { voice1Bacba, voice2Bacba, voice3Bacba, voice4Bacba }
            },

            new QuestStep
            {
                day = 3,
                questName = "Nhiệm vụ phụ",
                questDescription = "Bắt gà và đưa về chuồng.",
                stepType = QuestStepType.CatchChicken,
                targetItemId = "chick",
                requiredAmount = 3,
                isSideQuest = true,
                unlockAtMainStepIndex = 1
            },

            // Bác Ba – lần 2 (3 dòng → Voice5-7Bacba)
            new QuestStep
            {
                day = 3,
                questName = "Nhiệm vụ phụ",
                questDescription = "Nói chuyện lại với bác Ba.",
                stepType = QuestStepType.TalkToNPC,
                targetNPCId = "bac_ba",
                requiredAmount = 1,
                isSideQuest = true,
                unlockAtMainStepIndex = 1,
                dialogueLines = new string[]
                {
                    "Ôi cậu làm tốt quá, cảm ơn cậu nhiều nhé!",
                    "Lũ gà giờ đã ở yên trong chuồng rồi.",
                    "Ta đã chuẩn bị công thức nấu Cơm Gà cho cậu rồi đấy, hãy dùng nó để cho Gióng ăn nhé!"
                },
                voiceClips = new AudioClip[] { voice5Bacba, voice6Bacba, voice7Bacba }
            }
        };
    }

    // ─── NGÀY 4 ────────────────────────────────────────────────────
    private List<QuestStep> GetDay4Steps()
    {
        return new List<QuestStep>
        {
            // Già Làng – ngày 4 (5 dòng → Voice12-16GL)
            new QuestStep
            {
                day = 4,
                questName = "Nhiệm vụ ngày 4",
                questDescription = "Nói chuyện với Già Làng.",
                stepType = QuestStepType.TalkToNPC,
                targetNPCId = "village_elder",
                requiredAmount = 1,
                dialogueLines = new string[]
                {
                    "Nhiệm vụ hôm nay cũng như hôm qua.",
                    "Mà giặc Ân đã đến rất gần rồi.",
                    "Chúng ta phải chuẩn bị vũ khí cho Gióng.",
                    "Cậu hãy đến gặp bác thợ rèn ở rìa làng.",
                    "Bác ấy đang cần quặng sắt để chuẩn bị đúc ngựa và roi sắt đấy."
                },
                voiceClips = new AudioClip[] { voice10GL, voice11GL, voice12GL, voice13GL, voice14GL }
            },

            // Thợ Rèn – ngày 4 (4 dòng → Voice1-4ThoRen)
            new QuestStep
            {
                day = 4,
                questName = "Nhiệm vụ phụ",
                questDescription = "Gặp Bác Thợ Rèn.",
                stepType = QuestStepType.TalkToNPC,
                targetNPCId = "blacksmith",
                isSideQuest = true,
                unlockAtMainStepIndex = 1,
                requiredAmount = 1,
                rewardItem = pickaxeItem,
                rewardAmount = 1,
                rewardTiming = RewardTiming.TalkToNPC,
                rewardMessage = "Nhận được Cuốc Chim!",
                requireInventorySpace = true,
                dialogueLines = new string[]
                {
                    "Cậu ắt hẳn là người mà Già Làng nhắc đến.",
                    "Củi lửa đã sẵn sàng nhưng ta đang thiếu quặng sắt trầm trọng.",
                    "Cậu hãy cầm lấy cây cuốc chim này.",
                    "Ra mỏ đá phía sau làng đào một ít Quặng Sắt mang về kho giúp ta nhé!"
                },
                voiceClips = new AudioClip[] { voice1ThoRen, voice2ThoRen, voice3ThoRen, voice4ThoRen }
            },

            new QuestStep
            {
                day = 4,
                questName = "Nhiệm vụ phụ",
                questDescription = "Khai thác 2 quặng sắt và mang về kho",
                stepType = QuestStepType.CollectIron,
                targetItemId = "iron_ore",
                requiredAmount = 2,
                isSideQuest = true,
                unlockAtMainStepIndex = 1
            },

            new QuestStep
            {
                day = 4,
                questName = "Nhiệm vụ ngày 4",
                questDescription = "Nấu cơm và giữ cho thanh đói của Gióng trên 80% khi ngày kết thúc.",
                stepType = QuestStepType.SurviveUntilDayEnd,
                requiredAmount = 1
            }
        };
    }

    // ─── NGÀY 5 ────────────────────────────────────────────────────
    private List<QuestStep> GetDay5Steps()
    {
        return new List<QuestStep>
        {
            // Già Làng – ngày 5 (6 dòng → Voice17-22GL)
            new QuestStep
            {
                day = 5,
                questName = "Nhiệm vụ ngày 5",
                questDescription = "Nói chuyện với Già Làng.",
                stepType = QuestStepType.TalkToNPC,
                targetNPCId = "village_elder",
                requiredAmount = 1,
                rewardItem = axeItem,
                rewardAmount = 1,
                rewardTiming = RewardTiming.TalkToNPC,
                rewardMessage = "Nhận được Rìu!",
                requireInventorySpace = true,
                dialogueLines = new string[]
                {
                    "Tình hình nguy cấp rồi cậu trẻ ơi!",
                    "Giặc Ân đã áp sát biên thùy.",
                    "Làng ta cần gấp một lượng tre lớn để làm cọc phòng thủ.",
                    "Khu vực Rừng Tre ở phía Đông, cậu hãy mang rìu ra đó thu hoạch nhé.",
                    "À, sẵn có ống tre tươi, cậu có thể nấu món Cơm Lam Ống Tre cho Gióng.",
                    "Thằng bé giờ đã lớn bằng ngôi nhà, sức ăn kinh khủng lắm."
                },
                voiceClips = new AudioClip[] { voice16GL, voice17GL, voice18GL, voice19GL, voice21GL, voice22GL }
            },

            new QuestStep
            {
                day = 5,
                questName = "Nhiệm vụ phụ",
                questDescription = "Chặt 5 bó tre mang về kho.",
                stepType = QuestStepType.CollectBamboo,
                targetItemId = "bamboo",
                requiredAmount = 5,
                isSideQuest = true,
                unlockAtMainStepIndex = 1
            },

            new QuestStep
            {
                day = 5,
                questName = "Nhiệm vụ ngày 5",
                questDescription = "Nấu ăn và giữ cho thanh đói của Gióng trên 80% khi ngày kết thúc.",
                stepType = QuestStepType.SurviveUntilDayEnd,
                requiredAmount = 1
            }
        };
    }

    // ─── NGÀY 6 ────────────────────────────────────────────────────
    private List<QuestStep> GetDay6Steps()
    {
        return new List<QuestStep>
        {
            // Già Làng – ngày 6 (5 dòng → Voice23-27GL)
            new QuestStep
            {
                day = 6,
                questName = "Nhiệm vụ ngày 6",
                questDescription = "Nói chuyện với Già Làng.",
                stepType = QuestStepType.TalkToNPC,
                targetNPCId = "village_elder",
                requiredAmount = 1,
                dialogueLines = new string[]
                {
                    "Ngày mai là thợ rèn bắt đầu đúc vũ khí thần tốc rồi.",
                    "Hôm nay là ngày cao điểm để tích lũy tài nguyên.",
                    "Chúng ta phải dốc toàn lực!",
                    "Hãy vừa cho Gióng ăn, vừa vận chuyển thật nhiều Sắt và Tre vào kho dự trữ.",
                    "Sức ăn của Gióng hôm nay đã đạt đỉnh, cậu phải hoạt động hết công suất đấy!"
                },
                voiceClips = new AudioClip[] { voice23GL, voice24GL, voice25GL, voice26GL, voice27GL }
            },

            new QuestStep
            {
                day = 6,
                questName = "Nhiệm vụ ngày 6",
                questDescription = "Giữ thanh đói trên 80%, tích trữ ít nhất 5 quặng sắt và 10 bó tre.",
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

    // ─── NGÀY 7 ────────────────────────────────────────────────────
    private List<QuestStep> GetDay7Steps()
    {
        return new List<QuestStep>
        {
            // Già Làng – ngày 7 (2 dòng → Voice28-29GL)
            new QuestStep
            {
                day = 7,
                questName = "Nhiệm vụ ngày 7",
                questDescription = "Nói chuyện với Già Làng.",
                stepType = QuestStepType.TalkToNPC,
                targetNPCId = "village_elder",
                requiredAmount = 1,
                dialogueLines = new string[]
                {
                    "Hôm nay là ngày cuối cùng rồi.",
                    "Hãy gặp bác thợ rèn để biết mình cần làm gì nhé."
                },
                voiceClips = new AudioClip[] { voice23GL, voice29GL }
            },

            // Thợ Rèn – ngày 7 (7 dòng → Voice5-11ThoRen)
            new QuestStep
            {
                day = 7,
                questName = "Nhiệm vụ ngày 7",
                questDescription = "Nói chuyện với Bác Thợ Rèn.",
                stepType = QuestStepType.TalkToNPC,
                targetNPCId = "blacksmith",
                requiredAmount = 1,
                dialogueLines = new string[]
                {
                    "Giặc đã đến đầu làng rồi!",
                    "Thời khắc quyết định đã đến!",
                    "Tuy nhiên chúng ta thiếu quá nhiều sắt.",
                    "Cậu trẻ hãy giúp ta một tay.",
                    "Hãy đào và mang 10 viên sắt vào kho.",
                    "Để ta đúc ngựa sắt, roi sắt!",
                    "Song song đó, vẫn phải đưa cơm cho Gióng ăn no để chuẩn bị xuất quân!"
                },
                voiceClips = new AudioClip[] { voice5ThoRen, voice6ThoRen, voice7ThoRen, voice8ThoRen, voice9ThoRen, voice10ThoRen, voice11ThoRen }
            },

            new QuestStep
            {
                day = 7,
                questName = "Nhiệm vụ ngày 7",
                questDescription = "Giữ thanh đói trên 80% và tích trữ ít nhất 10 quặng sắt.",
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