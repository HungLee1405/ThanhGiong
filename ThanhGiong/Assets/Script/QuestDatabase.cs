using System.Collections.Generic;
using UnityEngine;

public class QuestDatabase : MonoBehaviour
{
    public List<QuestStep> GetQuestStepsForDay(int day)
    {
        switch (day)
        {
            case 1:
                return GetDay1Steps();

            case 2:
                return GetDay2Steps();

            case 3:
                return GetDay3Steps();

            case 4:
                return GetDay4Steps();

            case 5:
                return GetDay5Steps();

            case 6:
                return GetDay6Steps();

            case 7:
                return GetDay7Steps();

            default:
                return new List<QuestStep>();
        }
    }

    private List<QuestStep> GetDay1Steps()
    {
        return new List<QuestStep>
        {
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
                }
            },

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
                }
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
                questDescription = "Nói chuyện lại với mẹ Gióng.",
                stepType = QuestStepType.TalkToNPC,
                targetNPCId = "giong_mother",
                requiredAmount = 1,
                dialogueLines = new string[]
                {
                    "Tuyệt vời, cậu làm tốt lắm.",
                    "Tuy nhiên nhiêu đây là chưa đủ, con tôi giờ đây đang ăn rất nhiều.",
                    "Hãy nấu và đưa cơm cho tôi để tôi đưa cho nó ăn.",
                    "Đây là thanh đói, hãy giúp tôi giữ nó trên 80% khi hết ngày."
                }
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

    private List<QuestStep> GetDay2Steps()
    {
        return new List<QuestStep>
        {
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
                    "Thanh đói sẽ tụt nhanh hơn. Hãy cẩn thận."
                }
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

    private List<QuestStep> GetDay3Steps()
    {
        return new List<QuestStep>
        {
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
                    "Nhiệm vụ hôm nay cũng giống vậy.",
                    "À mà hình như lão Năm đang gặp khó khăn gì đó.",
                    "Cậu hãy ghé thăm kiểm tra xem."
                }
            },

            new QuestStep
            {
                day = 3,
                questName = "Nhiệm vụ phụ",
                questDescription = "Gặp lão Năm.",
                stepType = QuestStepType.TalkToNPC,
                targetNPCId = "old_man_nam",
                requiredAmount = 1,
                dialogueLines = new string[]
                {
                    "Ui gia, lão đây đã già rồi, sức đâu mà làm mấy chuyện này chứ.",
                    "À, cậu có phải là người mà Già Làng đã nói không?",
                    "Đám gà nhà tôi đã xổng chuồng chạy mất tiêu rồi.",
                    "Cậu hãy giúp tôi bắt chúng lại và bỏ vào khu vực chuồng được chứ.",
                    "Sau khi làm xong thì cậu có thể bắt gà nhà tôi mà nấu cơm gà cho Gióng ăn."
                }
            },

            new QuestStep
            {
                day = 3,
                questName = "Nhiệm vụ phụ",
                questDescription = "Giúp lão Năm bắt 3 con gà.",
                stepType = QuestStepType.CatchChicken,
                targetItemId = "chicken",
                requiredAmount = 3
            },

            new QuestStep
            {
                day = 3,
                questName = "Nhiệm vụ ngày 3",
                questDescription = "Nấu cơm và giữ cho thanh đói của Gióng trên 80% khi ngày kết thúc.",
                stepType = QuestStepType.SurviveUntilDayEnd,
                requiredAmount = 1
            }
        };
    }

    private List<QuestStep> GetDay4Steps()
    {
        return new List<QuestStep>
        {
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
                }
            },

            new QuestStep
            {
                day = 4,
                questName = "Nhiệm vụ phụ",
                questDescription = "Gặp Bác Thợ Rèn.",
                stepType = QuestStepType.TalkToNPC,
                targetNPCId = "blacksmith",
                requiredAmount = 1,
                dialogueLines = new string[]
                {
                    "Cậu ắt hẳn là người mà Già Làng nhắc đến.",
                    "Củi lửa đã sẵn sàng nhưng ta đang thiếu quặng sắt trầm trọng.",
                    "Cậu hãy cầm lấy cây cuốc chim này.",
                    "Ra mỏ đá phía sau làng đào một ít Quặng Sắt mang về kho giúp ta nhé!"
                }
            },

            new QuestStep
            {
                day = 4,
                questName = "Nhiệm vụ phụ",
                questDescription = "Khai thác 5 quặng sắt.",
                stepType = QuestStepType.CollectIron,
                targetItemId = "iron_ore",
                requiredAmount = 5
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

    private List<QuestStep> GetDay5Steps()
    {
        return new List<QuestStep>
        {
            new QuestStep
            {
                day = 5,
                questName = "Nhiệm vụ ngày 5",
                questDescription = "Nói chuyện với Già Làng.",
                stepType = QuestStepType.TalkToNPC,
                targetNPCId = "village_elder",
                requiredAmount = 1,
                dialogueLines = new string[]
                {
                    "Tình hình nguy cấp rồi cậu trẻ ơi!",
                    "Giặc Ân đã áp sát biên thùy.",
                    "Làng ta cần gấp một lượng tre lớn để làm cọc phòng thủ.",
                    "Khu vực Rừng Tre ở phía Đông, cậu hãy mang rìu ra đó thu hoạch nhé.",
                    "À, sẵn có ống tre tươi, cậu có thể nấu món Cơm Lam Ống Tre cho Gióng.",
                    "Thằng bé giờ đã lớn bằng ngôi nhà, sức ăn kinh khủng lắm."
                }
            },

            new QuestStep
            {
                day = 5,
                questName = "Nhiệm vụ phụ",
                questDescription = "Chặt 5 bó tre mang về kho.",
                stepType = QuestStepType.CollectBamboo,
                targetItemId = "bamboo",
                requiredAmount = 5
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

    private List<QuestStep> GetDay6Steps()
    {
        return new List<QuestStep>
        {
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
                }
            },

            new QuestStep
            {
                day = 6,
                questName = "Nhiệm vụ ngày 6",
                questDescription = "Giữ thanh đói trên 80%, tích trữ ít nhất 10 quặng sắt và 10 bó tre.",
                stepType = QuestStepType.SurviveUntilDayEnd,
                requiredAmount = 1
            }
        };
    }

    private List<QuestStep> GetDay7Steps()
    {
        return new List<QuestStep>
        {
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
                    "Một mình ta không thể vừa đập búa vừa làm nguội vũ khí kịp.",
                    "Cậu trẻ hãy giúp ta một tay.",
                    "Liên tục bưng Sắt vào lò, múc Nước đổ vào bể làm nguội.",
                    "Và đẩy bễ lò rèn thật đều tay để ta đúc xong ngựa sắt, roi sắt!",
                    "Song song đó, dân làng vẫn phải đưa cơm cho Gióng ăn no để chuẩn bị xuất quân đấy!"
                }
            },

            new QuestStep
            {
                day = 7,
                questName = "Nhiệm vụ ngày 7",
                questDescription = "Giữ thanh đói trên 80% và hoàn thành vũ khí 100%.",
                stepType = QuestStepType.ForgeWeapon,
                requiredAmount = 100
            }
        };
    }
}