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
->start


== start ==
~ speaker = ""
......
~ speaker = "我"
「你輸了。按照賭約，你必須讓我離開，而且永遠不准再纏著我。」
~ speaker = "“它”"
「哎呀~那只不過是我跟你開的小玩笑，你還當真了啊？」
~ speaker = "我"
「你...！」
~ speaker = "“它”"
「好啦好啦~不要用這麼兇的眼神看我，我放你們走就是了～」
#play_music NormalEndBGM
#awake
~ speaker = "引路人"
「這裡是......？」
~ speaker = "我"
「你終於醒了。」
~ speaker = "引路人"
「我這是...怎麼了？」
~ speaker = "我"
「你剛剛被“它”變成了木偶，我跟“它”做了個賭約，最後我贏了，我才能把你...我！我們救回來。」
~ speaker = "引路人"
「這樣啊...謝謝你。」
~ speaker = "“它”"
「好了！你們要怎麼道謝跟寒暄我都不管，趁我還沒反悔之前趕快離開，否則如果我後悔了，你們一個都跑不掉。」
~ speaker = "我"
「哼！我也不想繼續待在這個鬼地方了，記得遵守你的承諾，不准再繼續纏著我。」
~ speaker = "“它”"
「知道了啦~囉嗦死了...」
~ speaker = "我"
「我們走。」
#black_screen
#Hospital
~ speaker = ""
......
#back_screen
回到現實，在醫院醒來，父母十分焦急，見到他醒來後喜極而泣。
~ speaker = "媽媽"
「墨涅，你終於醒了。」
~ speaker = "爸爸"
「嚇死我們了……以後不要再讓我們擔心了，好嗎？」
~ speaker = "我"
「媽媽...爸爸...」
#black_screen
~ speaker = ""
不知道是不是因為經歷了這次的事件，讓父母悔悟
還是這一切本身就是一場夢，都是墨涅幻想出來的
墨涅的父母對他態度大轉變，墨涅不再被他爸媽責罵
考試考不好也只是簡單帶過就算了，還讓墨涅去自己決定想要學什麼。
同學們也不再排擠他，甚至還有主動來找他搭話的
墨涅後來也憑藉著自己優秀的藝術天份得到了同學跟父母的讚賞
最後他成功考上全國最頂尖的藝術大學。
#BackMountain
......
#pause_music
#back_screen
......
~ speaker = "“它”"
「我替你準備的大禮，還喜歡嗎？嘻嘻~」
「我只答應你不再纏著你，可沒說...你的人生，我不能動。」
#play_cg NormalEndCG
#black_end
......
#MainMeun
-> DONE

