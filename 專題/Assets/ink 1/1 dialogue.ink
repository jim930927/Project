VAR talked_to_boss = false
VAR journal_choices_done = false
VAR speaker = "???"
VAR hp = 4
EXTERNAL UnlockLetter()
EXTERNAL UnlockJournal()
EXTERNAL UnlockTalk()
EXTERNAL canStartBattle()


== CG ==
#play_cg
->start

== start ==
~ speaker = "我"
......
#play_music start_theme
這裡是……舞台？為什麼我會在這種地方？
那裡...好像有個人，先去問問怎麼離開這裡好了
-> END


== book_found ==
~ speaker = "我"
這是什麼......？

「這是...劇本？」

「先帶著吧，說不定會有用」
-> END


== boss_talk
{ talked_to_boss == false:
    -> boss_talk_first
- else:
    -> boss_talk_repeat
}


== boss_talk_first
~ speaker = "我"
「那個...請問你知道這是哪裡嗎？」

~ speaker = "神秘人"
「你...還記得...自己是誰嗎？」

~ speaker = "我"
「什麼？我是誰？」
「我是......」
「欸？我......是誰？我好像...想不起來了......」

~ speaker = "神秘人"
「看看四周…你會想起來的……」
「還有這個......你拿著......」

~ speaker = "我"
「這是......油燈？你給我這個幹什麼？」

~ speaker = "神秘人"
「保護好他...他將是你能否離開的關鍵」

~ talked_to_boss = true
#show_hp
-> END


== main_npc_talk
~ speaker = "???"
「欸欸！你知道B班的那個神經病嗎？」
「我好像知道！我記得他...姓什麼來著？我想一下......喔！好像是姓...墨？」
「喔對對對！聽說他好像有精神疾病的樣子！」
「真的假的？難怪！我上次好像有看到他在吃藥」
「以後遇到他要小心一點，免得他哪天一發病就攻擊人」
「天啊，傻X學校為什麼不開除他？讓我們這些普通人跟隨時都有可能會攻擊我們的神經病待在一起？」

~ speaker = "我"
* 「姓墨？是在說我嗎...」
    ~ hp += 1
    「我有精神疾病？」
    -> END
* 「精神疾病？」
    ~ hp -= 1
    「真令人討厭......」
    -> END
* 「好希望這樣的人也能夠跟大家好好相處...」
    「他有精神疾病應該連他自己也很不好受......」
    ~ speaker = ""
    【獲得線索 “木偶的對話”】
    -> END

== letter_content
~ speaker = "我"
「有個信封？說不定跟我的身份有關，打開來看看。」

~ speaker = ""
信封袋裡面放著一張字跡潦草、字裡行間還參雜著注音的信件。
是七歲的墨涅寫給未來自己的信——
-> END


== letter_choices
~ speaker = "我"
* 「墨涅...這是我的名字嗎？」
    ~ hp += 1
    「所以...這是我給我自己的信？啊…好想燒掉，感覺好羞恥......」
    -> END
* 「墨涅？沒聽過這個名字......」
    ~ hp -= 1
    「這個人究竟是誰呢？」
    -> END
* 「字寫得好醜......」
    「還有一堆注音，應該是小孩子寫的」
    ~ speaker = ""
    【獲得線索 “一封老舊的信封”】
    -> END

== journal_content
~ speaker = "我"
「這是……一本日記？」
翻開椅子上的日記
-> END

== journal_choices
{ journal_choices_done:
    -> END
- else:
    ~ speaker = "我"
    「這篇日記到後面就沒有任何記載了......」
    「......」
    「這真的是我寫的嗎...」
    * 「總感覺我好像不是很受歡迎...」
       ~ hp += 1
        「不受父母愛戴...也沒有真心的朋友」
        「真可笑...」
        ~ journal_choices_done = true
        -> END
    * 「這些人真可惡！我到底是招誰惹誰了」
        ~ hp -= 1
        「真想讓所有傷害過我的人都消失在這世界上...」
        「......」
        「我...為什麼會有這樣的想法......？」
        「真可怕......」
        ~ journal_choices_done = true
        -> END
    * 「陰廟...？所以...我去許願了？」
        「可是我為什麼一點印象都沒有？」
        【獲得線索 “日記殘頁”】
        ~ journal_choices_done = true
        -> END
}
== boss_talk_repeat
~ speaker = "神秘人"
「你…找到屬於自己的記憶了嗎……」
~ speaker = "我"
+ 「我想…我大概知道了……」
    -> after_boss_choice_1
+ 「還沒有……」
    -> after_boss_choice_2

== after_boss_choice_1
~ speaker = "神秘人"
{ canStartBattle():
    「那麼請你回答我幾個問題…回答完我自然會離開，大門也會開啟……」
    -> jump_to_battle
- else:
    「看起來你還沒有準備好...再多看看吧」
    -> END
}

== after_boss_choice_2
~ speaker = "神秘人"
「那就再多看看吧……」
-> END


== jump_to_battle
# jump_to_battle
-> END

