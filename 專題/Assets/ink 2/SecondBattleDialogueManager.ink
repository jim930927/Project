VAR current_clue = ""
VAR speaker = ""

=== start ===
~ speaker = "鬱"
「你...違反規則了」
~ speaker = "墨涅"
「很抱歉，但...為了找回我全部的記憶，我別無選擇」
~ speaker = "鬱"
「你就乖乖的遵守那些規則不好嗎？只要好好遵守規則，就能安安穩穩的過日子，這不就是你想要的嗎？」
~ speaker = "墨涅"
「這...真的是我想要的嗎？」
~ speaker = "鬱"
「我無論如何都會阻止你繼續往前！」
-> q1

=== q1 ===
~ speaker = "鬱"
「你看到的那些畫面、感受到的痛苦，不過是你腦子亂編的幻象，沒有任何意義。」

+ [那些都是真實的] #current_clue:Exam
    {current_clue == "Exam":
        -> correct_q1
    - else:
        -> wrong_clue_q1
    }

+ [那些都是想像的]
    -> wrong_answer_q1

=== correct_q1 ===
~ speaker = "墨涅"
「不，這些記憶不是幻象……我感受到的恐懼、孤獨，全都是真實的。你只是怕我面對它們。」
-> q2

=== wrong_answer_q1 ===
~ speaker = "鬱"
「沒錯，那些都是假的，忘掉它們才是對的。」
-> q2

=== wrong_clue_q1 ===
~ speaker = "鬱"
「這東西...根本沒辦法證明你說的話是對的。」
-> q2


=== q2 ===
~ speaker = "鬱"
「明明只要不去想、不去記得那些痛苦的回憶，乖乖聽從爸媽的話，就能成為“乖孩子”。」
「這樣的生活到底有哪裡不好？你為什麼非得去找到真相不可！」

+ [為什麼要去尋找真相] #current_clue:Journal2
    {current_clue == "Journal2":
        -> correct_q2
    - else:
        -> wrong_clue_q2
    }

=== correct_q2 ===
~ speaker = "墨涅"
「因為...我覺得他們並沒有我想得那麼壞...」
「至少...在這些日記殘頁裡可以看出來，他們還是很關心我的......」
-> q3

=== wrong_clue_q2 ===
~ speaker = "鬱"
「你回答不出來嗎？果然還是聽我的，乖乖當個乖孩子就好。」
-> q3


=== q3 ===
~ speaker = "鬱"
「真相？別傻了！那些全都是假象！如果他們真的愛你，幹嘛制定那麼多規則！」

+ [為什麼爸媽會突然性情大變] #current_clue:Journal1
    {current_clue == "Journal1":
        -> correct_q3
    - else:
        -> wrong_clue_q3
    }

=== correct_q3 ===
~ speaker = "墨涅"
「我猜...應該是因為這個吧...」
「他們一定有苦衷，只是我不知道而已。」
-> q4

=== wrong_clue_q3 ===
~ speaker = "鬱"
「這能證明什麼？你也不知道對吧？那就聽我的。」
-> q4


=== q4 ===
~ speaker = "鬱"
「要真有什麼苦衷，為什麼不能好好跟我談談？」
「難道有苦衷就能把孩子當出氣筒嗎？」
「這就是愛嗎？」

+ [我變成這樣，都是因為他們] #current_clue:Medical_record
    {current_clue == "Medical_record":
        -> correct_q4
    - else:
        -> wrong_clue_q4
    }

=== correct_q4 ===
~ speaker = "墨涅"
「這...你說的對......」
「如果沒有那些壓力，我也不用去看心理醫生了...」
-> q5

=== wrong_clue_q4 ===
~ speaker = "鬱"
「都是他們害的，才讓我生病，你忘了嗎？」
-> q5


=== q5 ===
~ speaker = "鬱"
「放棄吧...真相沒有意義。」

+ [繼續尋找真相] #current_clue:Letter2
    {current_clue == "Letter2":
        -> correct_q5
    - else:
        -> wrong_clue_q5
    }

+ [放棄尋找真相]
    -> wrong_answer_q5

+ [不知道]
    -> wrong_answer_q5

=== correct_q5 ===
~ speaker = "墨涅"
「這封信裡...是爸爸的字跡...」
「從這些字裡行間，我知道他討厭爺爺，但我不願意相信這就是全部。」
-> q6

=== wrong_answer_q5 ===
~ speaker = "鬱"
「我很開心你能做出明智的選擇。」
-> q6

=== wrong_clue_q5 ===
~ speaker = "鬱"
「這都是些什麼亂七八糟的東西？你想找到真相的動機就這樣而已啊？」
-> q6


=== q6 ===
~ speaker = "躁"
「愚蠢！就算找到真相又怎麼樣！爺爺也不會再回來了！」

+ [你說得對]
    -> wrong_answer_q6

+ [你說得不對] #current_clue:FamilyPortrait
    {current_clue == "FamilyPortrait":
        -> correct_q6
    - else:
        -> wrong_clue_q6
    }

=== correct_q6 ===
~ speaker = "墨涅"
「如果事情真像你說的那樣，那這張全家福也不會出現在這裡。」
-> q7

=== wrong_answer_q6 ===
~ speaker = "躁"
「看來你還是有點明辨是非的能力。」
-> q7

=== wrong_clue_q6 ===
~ speaker = "躁"
「我想你最好還是有點自知之明，不要在這裡丟人現眼。」
-> q7


=== q7 ===
~ speaker = "躁"
「你不懂！你根本不懂活在壓力下的感受！」

+ [保護自己！！！] #current_clue:PeaceTalisman
    {current_clue == "PeaceTalisman":
        -> correct_q7
    - else:
        -> wrong_clue_q7
    }

=== correct_q7 ===
~ speaker = "墨涅"
「這...這是媽媽給我的平安符...」
「媽媽...我明白了，謝謝妳。」
-> ending

=== wrong_clue_q7 ===
~ speaker = "躁"
「去死吧！」
~ speaker = "墨涅"
「好重的戾氣...再不趕快找東西掩護自己，我會被他吞噬的...」
-> ending


=== ending ===
~ speaker = "墨涅"
「放心吧…我一定會找到真相，就當是為了我，也為了你…我們……」
-> END

