VAR talked_to_boss = false
VAR journal_choices_done = false
VAR speaker = "???"
VAR hp = 3
VAR have_items =""

EXTERNAL UnlockLetter()
EXTERNAL UnlockJournal()
EXTERNAL UnlockTalk()
EXTERNAL canStartBattle()

== CG ==
~ speaker = " "
#play_cg NEWS
「......」
->start


== start ==
#play_music BadEndBGM
~ speaker = " "
......
另一方面，新的學期開始了
墨涅的事也在學校裡傳開，同學們議論紛紛，說法一大堆
但唯一不變的是『一定是學校後山上的陰廟搞的鬼』
這次事件也讓學校的陰廟傳聞越傳越廣，至於還有沒有人去許願就無人知曉了。
......
#back_screen
......
~ speaker = "墨涅"
「我希望...」
「所有欺負我的人...」
「都從這世界上消失......」
#play_cg BadEndCG
#black_end
-> main

== main
......
# MainMeun
->END