VAR speaker = "???"
VAR have_items = ""
VAR offense_rules = ""
VAR room = ""
VAR Unlock_door = false
VAR hp = ""
VAR bed_interact = 0
VAR ref_interact = 0
VAR foul = true
VAR Get_letter = ""
VAR CANincense = 0
VAR Get_incense = ""
VAR inc_interact = 0
VAR key_gold = false
VAR parent_explore = 0
VAR gate_key = false
VAR bed_chest = false
VAR quilt_end = false
VAR bed_top = false
VAR incense_open = false
VAR forcer_open = false
VAR incense_again = false
VAR sideboard = false
VAR sink_memory_done = false
VAR sink_memory_end_done = false
VAR memory4_done = false
VAR store_memory_end_done = false
VAR npc_guide_spawned = false

EXTERNAL UnlockDoor(door_id)
EXTERNAL SaveGame()
EXTERNAL ChangeBedImage(bed_type)
EXTERNAL ChangeToiletImage(toilet_type)
EXTERNAL MovePlayer(bed_side)
EXTERNAL SpawnObject(chest)
EXTERNAL OpenChestUI()
EXTERNAL OpenSafeUI()
EXTERNAL SpawnNPC(Guide) 
EXTERNAL HP_Add(hp)
EXTERNAL Get_Item(itemID)
EXTERNAL Get_Clue(clueID)
EXTERNAL ReplaceItem(oldItemID,newItemID)
EXTERNAL Get_fragments(fragmentID)

== CG ==
~ speaker = " "
#play_cg openingCG
「......」
->start

== start ==
~ speaker = " "
微弱晨光透過縫隙灑進，周圍安靜的可怕，唯獨鬧鐘發出了「滴答」的聲響。
~ speaker = "我"
「剛剛那是...我的記憶嗎？」
~ speaker = ""
抬頭看了看周圍
~ speaker = "我"
「這裡是...我家嗎？」
「......」
「不行...記憶很模糊！什麼都想不起來...」
「我得找到那些缺失的部分……否則，我連自己是誰都無法確定。」
#play_music second_theme
-> END

== trash_can
~ speaker = " "
一個垃圾桶，裡面是空的
-> END

== main_room ==
~ speaker = "我"
「門是鎖的...？鑰匙在哪」
+ 使用鑰匙開門
    {have_items == "key_room":
        -> have_key
    - else:
        -> no_key 
    }
+ 等等
    -> END

== have_key ==
~ UnlockDoor("main_room")
~ Unlock_door = true
~ speaker = ""
【使用道具：房間鑰匙】
使用鑰匙打開了門
->END

== no_key ==
~ speaker = "我"
「要先找到鑰匙才行...」
->END


== wardrobe ==
~ speaker = " "
裡面放著些許衣服，大多是長袖長褲
~ speaker = "我"
「衣服好少，而且怎麼沒有幾件短袖...」
「似乎可以躲進去...但現在沒這個必要」
~ speaker = " "
【按下E鍵即可躲藏】
->END


== mirror ==
~ speaker = " "
一面鏡子
~ SaveGame() 
站在鏡子前面，記住了自己現在的模樣
-> END


== Journal1 ==
~ speaker = "我"
「床頭櫃裡好像有什麼東西？」
床頭櫃裡放著幾張殘缺的日記
-> END

== journal_end ==
 ~ speaker = "我"
「這是之前那本日記缺少的前幾頁？」
「那些日記裡的聲音……像是在告訴我『你必須成為某種樣子』，可我真的想那樣嗎？如果那不是我想要的樣子，那日記裡的『我』又是誰？」
~ speaker = " "
【獲得線索：日記殘頁-1】
-> END

== note ==
~ speaker = " "
桌上放著一張紙條，上面詳細寫著各種家規
->END

== note_end ==
~ speaker = " "
【獲得道具“家規”】
~ speaker = "我"
「為什麼這裡會有這種東西？不知道觸發規則會怎樣……好像也跟出去無關。」
#SpawnGuideNPC
 ~ speaker = " "
一個幽暗身影出現在旁邊
#turn_back
~ speaker = "我"
「你是剛剛的那個......？」
~ speaker = "引路人"
「仔細看，紙條後面有甚麼東西」
~ speaker = ""
將紙條翻過來
~ speaker = "我"
「這後面似乎有寫過字...」
將紙條平攤在書桌上，手指撫過那些凹凸不平的痕跡
拿起一支桌上散落的鉛筆，輕輕在擦拭處來回塗抹，終於顯現隱藏文字——
~ Get_Item("rule_back")
~ speaker = ""
......
~ speaker = "我"
「這也是一條規則，跟前面的有什麼不同？ 難道盒子裡有什麼線索嗎...被藏在哪裡了？」
~ speaker = "引路人"
「繼續前進、繼續探尋，找到最真實的你吧」
「若你心中留有疑惑，來找我...或許我可以為你解答」
~ speaker = ""
~ bed_chest = true
【現在開始可以向AI引路人問問題（他只會回答遊戲相關的問題）或許他知道一些重要的線索】
~ npc_guide_spawned = true
->END


== bed ==
~ speaker = "我"
// 檢查是否超過互動次數
{ bed_interact >= 2 : 
    「好像沒有需要調查的地方了」
    -> END
- else:
    { bed_chest == true: 
        -> bed_inter
    - else:
        ->bed_again
    }  
}  

== bed_inter
{ bed_top == true: 
    * 查看床底下
        ~ MovePlayer("bed_side")
        ~ SpawnObject("chest")
        床底下藏著箱子，將箱子拿了出來，上面有一個4位數密碼鎖
        ~ speaker = "我"
        ~ bed_interact += 1
        「鎖住了...密碼是多少呢...是某個日期嗎」
        -> END
- else:
    * 查看被子
        ~ speaker = " "
        ~ ChangeBedImage("bed_quilt")
        ~ bed_interact += 1
        床上凌亂的被子，留有些許溫度
        -> quilt
    * 查看床底下
        ~ MovePlayer("bed_side")
        ~ SpawnObject("chest")
        ~ bed_interact += 1
        床底下藏著箱子，將箱子拿了出來，上面有一個4位數密碼鎖
        ~ speaker = "我"
        「鎖住了...密碼是多少呢...是某個日期嗎」
        -> END
    }  

== bed_again
{ quilt_end == true: 
    ~ speaker = ""
    床上還留有些許餘溫
    ->END
- else:
    * 查看被子
        ~ speaker = " "
        ~ ChangeBedImage("bed_quilt")
        ~ bed_interact += 1
        床上凌亂的被子，留有些許溫度
        -> quilt
}  


== quilt ==
~ speaker = "我"
* 整理被子
    #no_foul
    ~ ChangeBedImage("bed_neat")
    ~ foul = false
    ~ bed_top = true 
    ~ quilt_end = true
    「不整理的話...總感覺會有不好的事發生」
    ->END
* 放著不動
    #foul
    ~ foul = true
    ~ bed_top = true 
    ~ quilt_end = true
    「還是放著不動吧...應該...會沒事吧」
    ->END
    
== quilt_again

->END

== chest ==
~ speaker = "我"
「箱子的密碼是多少呢...」
~ OpenChestUI()
-> END

== chest_open ==
#play_cg chestCG
->chest_inside

== chest_inside ==
~ speaker = "我"
......
......
「1月23日...我的生日...」
~ speaker = ""
裡面放著一本繪本、病歷、獎狀
翻開其中畫著睡蓮池的一頁時，一把鑰匙掉了出來
當指尖碰觸到一把金屬鑰匙，上面泛著微光，似乎被時間磨平了棱角。
~ speaker = "我"
「這些東西……是我的嗎？」
~ speaker = "引路人"
「用它，解開這扇門的鎖。繼續尋找你失去的記憶吧。」
~ speaker = ""
~ Get_fragments("honor")
【獲得道具：房間鑰匙】
【獲得線索：墨涅的繪本、一份病歷、幾張藝術比賽的獎狀】
【獲得記憶碎片1/8：不受待見的榮譽】
->END



== enemy ==
~ speaker = "我"
「！！！」
{foul == true:
    -> offense
- else:
    -> no_offense
    }
-> END

== offense ==
~ speaker = "?"
「你……不是個乖孩子。這樣……會讓你受傷的。」
#Enemy_disappear
~ speaker = "我"
~ HP_Add(1)
「剛剛那是......?」
「......」
「觸犯規則果然會有危險的事嗎...」
「那聲音……像是我腦海裡最嚴厲的部分，在審判我……」
->END

== no_offense ==
~ speaker = "?"
「你……有聽話。聽話……才不會被傷害。」
#Enemy_disappear
~ speaker = "我"
「剛剛那是......?」
「......」
「觸犯規則果然會有危險的事嗎...」
「但他的語氣……不像是在威脅，反而像是在保證我安全。可這種安全……是不是意味著要放棄什麼？」
「......」
「我為什麼會這麼害怕……是因為我不想被懲罰，還是因為我怕失去他們的認同？」
#keep_music
->END

== get_catch
~ speaker = "??"
「觸犯規則...要受到懲罰」
~ HP_Add(-1)
~ speaker = "我"
「糟了......」
#GameOver1:TriggerRule
->END

== warehouse
~ speaker = " "
倉庫門緊鎖，似乎需要一把鑰匙。
->END

== shrine_hall
~ speaker = " "
香爐裡只有幾根燒盡的香。
->END

== TV
~ speaker = " "
電視打開著，畫面上循環播放著像是雪花一樣的雜訊。
->END

== sofa
~ speaker = " "
坐起來很舒服老舊的沙發，上面有長期使用的痕跡
->END

== Dining_room
~ speaker = " "
大多數家庭常見的大紅色餐桌，似乎使用了很久，已經開始掉色了。
->END

== calendar
~ speaker = " "
牆上的月曆翻到了8月。
->END

== clock
~ speaker = " "
時間指向7:25，看起來像是一個悲傷的表情。
->END


== toilet ==
~ speaker = ""
白色陶瓷馬桶，因較老舊，已逐漸泛黃
+ 打開
    ~ ChangeToiletImage("toilet_open")
    裡面比想像中乾淨，沒有什麼髒污跟異味。
    ->END
+ 關著
    ~ ChangeToiletImage("toilet_close")
    ~ speaker = "我"
    「馬桶裡應該不會有什麼重要線索，還是讓它蓋吧。」
    ->END
    
    
== tub
~ speaker = " "
浴缸上佈滿水痕，看起來用了好幾年了。
->END

== sink
~ speaker = "我"
洗手台裡放滿了水，水上面放了一張被水浸濕的信封，還有幾根不知道是甚麼動物的毛
~ speaker = "我"
「為什麼這洗手台上有這麼多的毛啊...？」
~ speaker = " "
用手觸碰了水裡的毛
#memory1
#pause_music
->sink_memory

== sink_memory
~ speaker = " "
「......」
洗手台坐著一隻很髒的小貓
小貓意外的很安分，沒有抗拒洗澡
~ speaker = "回憶裡的我"
「怎麼搞得這麼髒的...」
「哈啾！...奇怪怎麼一直打噴嚏」
~ speaker = "貓咪"
「喵」
~ speaker = "回憶裡的我"
「安靜點...要是被發現，我跟你都會完蛋」
「......」
「你跟我還真像啊...」
#father_appear
~ speaker = "回憶裡的我"
「！！！」
#turn_back
~ speaker = "爸爸"
「你在幹什麼！」
~ speaker = "回憶裡的我"
「爸...那個...我......」
~ speaker = "爸爸"
「不是跟你說過不准帶野貓野狗回家的嗎？你不知道牠們有多髒嗎？身上可能會有一堆細菌或是跳蚤之類的，你怎麼怎麼講都講不聽！」
~ speaker = "回憶裡的我"
「可是...」
~ speaker = "爸爸"
「沒有可是！把牠給我！回去你房間，你被禁足了！」
#sink_memory_end
->sink_memory_end

== sink_memory_end
~ sink_memory_done = true
~ sink_memory_end_done = true
#keep_music
~ speaker = " "
......
~ speaker = "我"
「記憶中的我違反了規則……」
低下頭，看著自己濕透的手指。
「那時候的我……真的做錯了嗎？」
「他們說那是髒的、危險的……可我只看到牠在發抖、需要幫忙。」
「如果幫助一條生命是錯的，那我又該怎麼分辨什麼才是對的？」
抬起頭，看著鏡中被水霧模糊的自己。
「也許我害怕的……不是規則本身，而是當我違反它們時，就不再是他們心中的『好孩子』。」
「所謂的規則……原來都是反映我在現實中被禁止的事啊……」
（撿起水中的信封）
->END

== water_latter
「這信封完全濕掉了...如果強行打開，絕對會破掉，要想個辦法把它變乾...」
~ speaker = ""
~ Get_letter = "water_letter"
【獲得道具：被水浸濕的信封】
->END

== refrigerator
~ ref_interact += 1
~ speaker = " "
普通的冰箱。
{ ref_interact > 2:
    ~ speaker = "我"
    「好像沒有需要調查的地方了」
    -> END
- else:
    * 打開冰箱
        #memory2
        #pause_music
        「......」
        ->refrigerator_memory
    * 不打開冰箱
        ~ speaker = "我"
        「現在肚子不餓，不用找吃的」
        「嗯？冰箱上好像貼著什麼東西」
        「這是...又一個日記殘頁？」
        ~ Get_Clue("Journal2")
        ->Journal2
}  

== refrigerator_memory
~ speaker = "回憶裡的我"
    「看看裡面有什麼好吃的」
    #mother_appear
    ~ speaker = "媽媽"
    「墨涅你在幹嘛？」
    ~ speaker = "回憶裡的我"
    #turn_back
    「啊...我......」
    ~ speaker = "媽媽"
    「我不是警告過你半夜不准吃東西了嗎？」
    ~ speaker = "回憶裡的我"
    「可是...我讀書讀到現在...肚子餓了嘛......」
    ~ speaker = "媽媽"
    「......」
    「那就趕快去睡覺！剩下的明天再讀！」
    ~ speaker = "回憶裡的我"
    「好......」
    #refrigerator_memory_end
    ->refrigerator_memory_end

== refrigerator_memory_end
#keep_music
~ speaker = ""
......
~ speaker = "我"
「從那之後...我好像再也沒在半夜跑到廚房了...」
「我一直認為，他們的規則只是為了讓我服從……」
「可那天的媽媽，似乎真的只是怕我累壞……」
「或者，我只是想把每一次的限制都解讀成惡意，這樣我才有理由反抗。」
->END

== Journal2
~ speaker = "我"
「......」
「爸跟媽...常常在吵架？」
「該不會...就是因為他們長期處的不愉快，才把氣全部出在我身上吧...」
「那我到底算什麼...我不是他們的孩子嗎？為什麼我要承擔他們不滿的情緒？他們有尊重過我嗎...」
「在他們眼裡...我究竟是他們的親生骨肉，還是可以隨意任由他們出氣的沙包？」
~ speaker = " "
【獲得線索：日記殘頁-2】
->END

== kitchen_sink
~ speaker = " "
水池裡有一點水珠，似乎最近有使用過，底下的櫥櫃很空似乎可以躲藏。
【按下E鍵即可躲藏】
-> END



== gas_stove
瓦斯爐上面放著一個平底鍋，上面還殘留了些許溫度，似乎前一段時間使用過。
{Get_letter == "water_letter":
    -> water_letter
- else:
    -> END
    }

== water_letter
~ Get_letter = "dry_letter"
~ speaker = "我"
「用火把信封烤乾試試看吧...」
~ speaker = " "
打開瓦斯爐把信封放置於火上面數公分高的地方進行火烤，直到信封完全乾燥
~ speaker = "我"
「這樣就可以了」
~ ReplaceItem("water_letter", "dry_letter")
~ speaker = " "
打開信封
~ speaker = "我"
「......貓毛過敏...原來家裡禁止帶動物回家是因為我嗎...」
「那天我只記得自己被罵、被禁足……卻沒想過，也許他們是真的在擔心我」
「如果這是真的……那我一直以來的怨恨，會不會有一部分，是誤會？」
「可是……如果他們是愛我的，為什麼還要用懲罰代替解釋？...他們口中的保護，為什麼要讓我覺得自己像犯了罪？」
「也許……真相，並不會讓我感到好受」
~ speaker = ""
~ Get_fragments("care")
【獲得線索：一封老舊的信封-2】
【獲得記憶碎片2/8：未曾解釋的擔心】
->END

== knife_holder
~ speaker = ""
上面放著三種不同款式的菜刀
~ speaker = "我"
「奇怪...為什麼我看到這個菜刀會有種想拿的衝動......？」
* 拿起
    #play_cg knifeCG
    #pause_music
    「......」
    #keep_music
    ->END
+ 算了
    「沒這個必要，還是算了吧」
    ->END

~ room = "洗衣間"
== cloth
~ speaker = ""
各式的衣物掛在了繩子上，基本上都乾了
~ speaker = "我"
「這件...應該是我的學校外套，口袋裡面好像有什麼東西...」
「這是...缺失的日記？」
~ speaker = ""
懸掛在曬衣繩上的學校外套裡有幾張日記殘頁。
->END

== cloth_end
~ Get_fragments("scars")
【獲得線索：日記殘頁-4】
【獲得記憶碎片3/8：校服上的傷痕】
->END

== cloth_wash
~ speaker = ""
普通的洗衣機
->END

== clothes_basket
~ speaker = " "
衣物堆積在了籃子裡。
~ speaker = "我"
「堆了好多衣服...這到底多久沒清理了啊？」
~ speaker = " "
在衣物堆裡面翻找起來
找到被揉皺的紙團
把紙團打開，裡面掉出一把舊鑰匙
紙團上面寫著「百物歸處，木門為鎖，灰塵作守。」
【獲得道具：不明的鑰匙、揉皺的紙團】
->END

== clothes_basket_end
~ speaker = "我"
「這紙團是指這把鑰匙對應的鎖嗎...」
~ speaker = " "
後面出現了黑影
~ speaker = "引路人"
「你找到了新的線索了啊...」
~ speaker = "我"
「！！！」
~ speaker = "我"
* 「Ｘ！你有什麼毛病啊！」
    #turn_back
    ~ HP_Add(1)
    ~ speaker = "我"
    「可以不要每次都無聲無息地突然出現接著又莫名其妙地消失嗎，下次出現可以給我一點心理準備嗎？」
    ~ speaker = "引路人"
    「我只是出來提醒你，要謹記你房間裡那張寫著家規的紙條」
    ~ speaker = "我"
    「你還敢說...都是你！害我不小心觸犯到了規則！」
    ~ speaker = "引路人"
    「小心！」
    #Enemy_appear
    #turn_left
    ~ speaker = "??"
    「你...觸犯了規則......」
    他的聲音低沉，像是從我胸腔深處傳出。
    ~ speaker = "??"
    「別去看……別去想……那些記憶只會讓你痛苦。」
    ~ speaker = ""
    這語氣不像威脅，更像勸阻。
    ~ speaker = "我"
    「他在擋我……可為什麼，我從他的語氣中我感覺到了害怕？」
    ~ speaker = "引路人"
    「現在不是說這個的時候，快跑！找個地方躲起來」
    ->chase
* 「怎麼又是你」
    #turn_back
    ~ HP_Add(-1)
    ~ speaker = "我"
    「你一直這樣跟蹤我到底有什麼目的？」
    ~ speaker = "引路人"
    「我只是出來提醒你，要謹記你房間裡那張寫著家規的紙條」
     ~ speaker = "我"
    「我的事不用你來關心，我自己可以解決」
     ~ speaker = ""
    語畢，引路人又消失的無影無蹤了
     ~ speaker = "我"
    「真是個奇怪的傢伙。算了！我還是趕快去其他地方找找看有沒有其他線索」
    ->END
* 「你怎麼總是神出鬼沒的」
    #turn_back
    ~ speaker = "引路人"
    「我只是出來提醒你，要謹記你房間裡那張寫著家規的紙條」
    ~ speaker = "我"
    「知道了...」
    語畢，引路人又消失的無影無蹤了
    ~ speaker = "我"
    「真是個奇怪的傢伙。算了！我還是趕快去其他地方找找看有沒有其他線索」
    ->END

== chase
#start_chase
#play_music running
->END

== gone
~ speaker = "我"
「......」
「好像走了...」
#play_music second_theme
->END

== storehouse
~ speaker = ""
倉庫門緊鎖，似乎需要一把鑰匙。
+ 使用鑰匙開門
    {have_items == "key_unknow":
        -> have_store_key
    - else:
        -> no_store_key 
    }
+ 等等
->END

== have_store_key ==
~ UnlockDoor("storehouse")
~ Unlock_door = true
~ speaker = ""
【使用道具：不明的鑰匙】
使用鑰匙打開了門
->END

== no_store_key ==
~ speaker = "我"
「要先找到鑰匙...」
->END

== storeroom
{ memory4_done:
    -> END
}
#memory4
#pause_music
~ speaker = ""
#turn_left
......
~ speaker = "媽媽"
「墨涅，今天段考的考卷呢？」
「考幾分？拿出來讓我看看。」
~ speaker = "媽媽"
「考這什麼分數」
「為什麼表哥每一科都能考滿分，你就不能？」
~ speaker = "回憶中的我"
「因為我不是他！」
~ speaker = "媽媽"
「一天到晚除了畫畫，你還會做什麼？」
~ speaker = "回憶中的我"
「我有在讀書，但你們都只覺得我一直在畫畫！」
~ speaker = "媽媽"
「有在讀書？ 有在讀還考這樣的成績？ 不要開玩笑了！」
~ speaker = "回憶中的我"
「我真的有！」
~ speaker = "媽媽"
「每天都只會畫那些有的沒有的東西，如果你把那些時間放在課業上怎麼可能考不了滿分！」
「不把書讀好你以後怎麼辦？ 準備去工地搬磚嗎！」
~ speaker = "回憶中的我"
「不是...這樣的...」
~ speaker = "媽媽"
「這些東西我小時候隨便就能學會了，為什麼你就是學不會？！」
~ speaker = "回憶中的我"
「我真的不知道該怎麼學！」
~ speaker = "媽媽"
「還敢頂嘴？！ 我看你就是日子過得太好了，你就待在倉庫裡，直到你徹底反省吧！」
#store_memory_end
#Exam_appear
->store_memory_end


== store_memory_end
~ memory4_done = true
~ store_memory_end_done = true 
~ speaker = ""
......
~ speaker = "我"
「我…不是沒用的孩子…真的…對不起…求你們…不要…不要再把我…關起來了…」
#black_screen
~ speaker = ""
#lay_down
「他們理想中的『我』……是認真讀書、成績優秀、不頂嘴的孩子……」
「可我記憶中的『我』……是喜歡畫畫、會抱小貓、會在半夜偷吃東西的孩子……」
「這兩個人……哪一個才是真的？」
#guide_appear
#enemy_appear
~ speaker = ""
......
#back_screen
~ speaker = "鬱的化身"
「規則……是不能違反的……」
~ speaker = "引路人"
「別做得太過火，別忘了，只有他找到自我，我們才有機會脫離 “祂” 的掌控。」
~ speaker = "鬱的化身"
「那...就更不能讓他去送死了。只要...他別去面對，像以前一樣…做個聽話的小孩…我們就能保全自己...」
「我會...不擇手段的阻止他...」
~ speaker = ""
他的手指微微顫抖——就像真的怕失去什麼。
~ speaker = "鬱的化身"
「因為……我知道那些記憶有多痛……我不想讓他再經歷一次。」
~ speaker = "引路人"
「難道...你想要一輩子活在 “祂” 的掌控下嗎。」
#EnemyNPC_disspear
~ speaker = " "
......
~ speaker = " "
引路人低下身拍了拍墨涅的身體
~ speaker = "引路人"
「醒醒」
~ speaker = "我"
「......」
~ speaker = ""
* 不想醒過來
    ~ speaker = "我"
    「這裡……很安靜，沒有人責罵，也沒有人逼我面對那些畫面……」
    「如果……我就一直這樣待下去……會不會比較輕鬆？」
    低頭，看見自己的手被細細的線牽住——線的另一端，消失在無盡的黑暗中。
    #GameOver2:LastDream
    ->END
* 醒過來
    #wake
    #keep_music
    ~ speaker = ""
    感覺有人在拍我便悠悠醒轉
    迷迷糊糊的睜開眼小聲地道
    ~ speaker = "我"
    #turn_left
    「誰？」
    ~ speaker = "引路人"
    「是我」
    ~ speaker = "我"
    「我這是...怎麼了？」
    ~ speaker = "引路人"
    「你從幻覺中再次體會到了當時的痛苦，承受不住...所以昏倒了」
    ~ speaker = "我"
    「......」
    ->wake
    
== wake
~ speaker = "我"
*「那...剛才那些...是真的嗎？」
    ~ speaker = "引路人"
    「是不是你自己心裡明白」
    ~ speaker = "我"
    「什麼意思？」
    ~ speaker = "引路人"
    「我能說的都已經說完了，剩下的只能靠你自己了」
    ~ speaker = "我"
    「可是我該怎麼做？」
    ~ speaker = "引路人"
    「繼續找回你的記憶吧，當你回想起一切、找回你的自我的時候，你就能得到答案。」
    ~ speaker = "我"
    (他說得輕描淡寫……可那畫面裡的痛，比我現在的心跳還真實。)
    「看來...只能繼續往前了......」
    #GuideNPC_disspear
    ->END
* 「不管那是幻覺還是現實」
    ~ HP_Add(1)
    ~ speaker = "我"
    「我想那一定跟我為什麼會來到這鬼地方有關，我一定要調查清楚」
    ~ speaker = "引路人"
    「繼續找回你的記憶吧，當你回想起一切、找回你的自我的時候，你就能得到答案。」
    ~ speaker = "我"
    (如果那是假的，那我又為什麼會流淚？)
    (如果那是真的，那現在的我……是那時候的延續，還是另一個人？)
    「看來...只能繼續往前了！」
    #GuideNPC_disspear
    ->END
*「我不想再看到那些了…拜託…」
    ~ HP_Add(-1)
    ~ speaker = "引路人"
    「......」
    「很抱歉...但...這些事情...都是曾經的你經歷過的事...你現在選擇逃避...之後還是得面對」
    ~ speaker = "我"
    「為什麼...我的父母要這樣對我？」
    ~ speaker = "引路人"
    「繼續找回你的記憶吧，當你回想起一切、找回你的自我的時候，你就能得到答案。」
    #GuideNPC_disspear
    ~ speaker = "我"
    「......」
    ->END


== exam_papers
~ speaker = "我"
~ Get_fragments("expect")
「這是剛剛幻覺中出現的考卷...」
~ speaker = ""
【獲得線索：一張寫著81分的數學考卷】
【獲得記憶碎片4/8：父母的期望】
->END



== carton
~ speaker = "我"
「這大小...似乎可以躲進去」
~ speaker = ""
【按下E鍵即可躲藏】
->END

== safe
~ speaker = "我"
「保險箱?...密碼會是多少...?」
~ OpenSafeUI()
->END

== safe_open
{ sideboard == true: 
    -> enough
- else:
    -> not_enough 
    }


== not_enough
~ Get_Clue("Letter2")
~ speaker = ""
......
~ speaker = "我"
「這是……我爸寫的嗎？」
「“那老不死的”？是在說誰？」
「這到底是怎麼回事？難不成我爸心裡有個他很討厭的人嗎？」
「……」
「或許這會是個很重要的線索，先帶在身上吧。」
「這裡還有爸媽房間的備用鑰匙？或許只要進去那裡…我就能找到一切我想了解的真相……」
#StoryNPC
~ speaker = "引路人"
「你找到了很重要的東西。」
~ speaker = "我"
「嗯！這是我爸媽房間的備用鑰匙，有了它我就能進到我爸媽的房間裡了，那裡應該會有很多我想知道的真相。」
~ speaker = "引路人"
不過我看你現在似乎有部分記憶沒有找回，沒準備好的話不要隨便違反規則進爸媽房間，否則你會遇到不好的事。」
~ speaker = "我"
「好，我知道了。」
#StoryNPC_disspear
~ speaker = ""
~ Get_fragments("lied")
【獲得道具：父母房間的備用鑰匙】
【獲得線索：一封字跡潦草的書信】
【獲得記憶碎片5/8：父親的謊言】
->END

== enough
~ Get_Clue("Letter2")
~ speaker = ""
......
~ speaker = "我"
「這是……我爸寫的嗎？」
「“那老不死的”？是指爺爺嗎？」
「這到底是怎麼回事？難不成我爸很討厭爺爺？」
「怎麼會這樣呢…從在客廳裡撿到的日記來看，爺爺應該是個很和藹、慈祥又很疼我的人，爸爸為什麼這麼討厭他？」
「該不會…爺爺當初會離開，其實是因為老爸的關係？」
「……」
「不行，這件事我必須調查清楚才行，這是…爸媽房間的備用鑰匙？或許只要進去那裡…我就能找到一切我想了解的真相……」
#StoryNPC
~ speaker = "引路人"
「找到重要的線索了吧」
~ speaker = "我"
「嗯......感覺當年爺爺會離開，並不像爸爸口頭上說的那麼簡單...我想...去找到這一切的真相...」
~ speaker = "引路人"
「不過...在還沒準備好之前，不要隨便違反規則進爸媽房間，否則…你可能會遇到不好的事」
~ speaker = "我"
「對不起，但我非去不可，這件事我一定要查清楚」
~ speaker = "引路人"
「好吧…那祝你好運，但我要先提醒你，去了之後就無法回頭，同時一切的命運皆掌握在你手中，望你好自為之」
#StoryNPC_disspear
~ speaker = ""
~ Get_fragments("lied")
【獲得道具：父母房間的備用鑰匙】
【獲得線索：一封字跡潦草的書信】
【獲得記憶碎片5/8：父親的謊言】
->END


== incense
~ speaker = ""
~ Get_incense = "incense"
抽屜裡放著一包香，香的下面放著一張寫著使用指南的紙
->END

== incense_end
~ speaker = "我"
~ Get_incense = "incense"
「香？怎麼會在這裡？」
「還有...使用指南?」
->END


== incense_burner
~ speaker = "我"
{ Get_incense == "incense": 
    { CANincense == 0:
    -> incense_burned
       - else:
            -> USEincense 
        }
   - else:
        -> no_incense 
    }
    
== no_incense
香爐裡只有幾根燒盡的香。
->END

== incense_burned
~ speaker = "我"
「感覺我以前的生活好像過得不太好...」
「爸媽給我的壓力就像山一樣壓得我喘不過氣來，我卻沒有能力反抗」
「就像是一隻提線木偶一樣隨意任人操控」
「......」
「看來...有些事還是只能請神明幫忙才行......」
~ speaker = ""
（面向神桌）
~ speaker = "我"
「拜拜的時候，拿香的規矩也是很重要的...」
「以前跟爸爸媽媽一起拜拜時，他們總是叮囑我拿的香數量不同有不同的涵義...」
「我究竟該拿幾支香來拜呢？」
~ CANincense = 1
->END


== USEincense
~ speaker = "我"
~ inc_interact += 1
{ inc_interact > 5:
    「不需要再拜拜了」
    -> END
- else:
    ~ speaker = "我"
    { incense_again == true:
    「不需要再拜拜了」
    -> END
    - else:
        ->use_incense
    }  

}  



== use_incense
~ speaker = "我"
「該拿幾支香來拜呢？」
~ speaker = ""
*【一炷香】
    「如果我想請神明幫忙，只點燃一支香可能不夠......」
    ->END
*【三炷香】
    ~ HP_Add(1)
    ~ speaker = "我"
    「我...好像只求過一次…那是我唯一主動去祈求神明的時候。」
    「......」
    「可那真的是我想要的嗎？還是……只是因為我想滿足他們的期待？」
    「連祈願的方式……都要有人告訴我『正確』的做法。
    可是，神明在意的……真的是香的數量，還是我祈求時的心意？」
    ~ speaker = ""
    #burn
    （拿起神桌上的打火機點亮了手中的香，拜三拜，向神明祈求，隨後把香插入香爐內）
    #open_forcer
    （角落放著祖先牌位的櫃子發出了咖噠一聲）
    ~ speaker = "我"
    「也許...我早就忘了自己想求什麼，只記得該怎麼做才能讓他們滿意。」
    ~ incense_open = true
    ~ forcer_open = true
    ~ incense_again = true
    ->END
*【五炷香】
    ~ HP_Add(-1)
    ~ speaker = "我"
    「節日或是法會才會用到，這好像跟我想祈求的事情無關......」
    ->END
*【七炷香】
    ~ speaker = "我"
    「調轉生死或命運走向，我應該還不至於用到這種地步吧......」
    ->END
*【九炷香】
    ~ HP_Add(-1)
    ~ speaker = "我"
    「我只不過是想跟神明祈求一些事而已，不用這麼大手筆......」
    ->END


== god_forcer
~ speaker = ""
打開櫃子，裡面放著一把金色的鑰匙，上面寫著 「櫥櫃鑰匙」
~ Get_Item("key_gold")
~ key_gold = true
裡面還放著一個平安符，上面寫著「保佑闔家平安」
#memory3
#pause_music
->amulet_memory


== amulet_memory
#keep_music
~ speaker = ""
......
#change_sprite
~ speaker = "媽媽"
「墨涅，這個平安符你拿著」
「這是媽媽剛剛跟廟公求來的符，它經過了太子爺的法力加持，可以保你一生平安」
~ speaker = "回憶中的我"
「好~」
#amulet_memory_end
-> amulet_memory_end

== amulet_memory_end
~ speaker = ""
#change_sprite_back
......
~ speaker = "我"
「真是諷刺啊。」
「你們希望我平安，可又一次次把我逼到崩潰的邊緣…」
「這算什麼…愛嗎？ 還是單純把我當作一種工具?」
「如果平安只是指活著、不生病、不出事……那我一直以來的那些痛苦，又算什麼？你們的『平安』，是不是只是希望我乖乖待在你們規劃好的籠子裡？」
~ speaker = ""
拿起平安符
~ Get_Clue("amulet")
~ Get_fragments("desire")
#forcer_end
【獲得線索：平安符（可抵擋一次攻擊）】
【獲得記憶碎片6/8：母親的願望】
->END

== cabinet
~ speaker = ""
鞋櫃裡有很多雙鞋
~ speaker = "我"
「有好幾雙鞋子特別髒...」
「嗯？鞋櫃裡好像有幾張紙...」
->END


== Journal5
~ speaker = ""
~ Get_fragments("silence")
【獲得線索：日記殘頁-5】
【獲得記憶碎片7/8：鞋舌下的沉默】
->END

== glass_cabinet
{key_gold == true:
    ->inside
-else:
->outside
}

->END

== inside
~ speaker = ""
~ key_gold = false
用神明廳得來的金鑰匙打開客廳的玻璃櫥窗。
#use_key_gold
裡面有一張童年出遊時的全家福照片、幾張日記殘頁
~ Get_Clue("FamilyPortrait")
~ Get_Clue("Journal3")
~ Get_fragments("memory")
~ speaker = "我"
......
「這是之前那本日記缺少的前幾頁？」
「這些畫面……笑容、擁抱、旅行……
和我記憶裡那些吼聲、懲罰、鎖上的門……是同一個家嗎？」
「如果兩個版本的家都是真的……那我到底生活在哪一個“家”裡？」
「也許……這才是最可怕的地方——我不知道自己該懷念哪一個家。」
~ sideboard = true
【獲得線索：全家福、日記殘頁-3】
【獲得記憶碎片8/8：記憶中的模樣】
->END

== outside
~ speaker = " "
櫥櫃緊鎖著，裡面放著一幅全家福，是小時候的墨涅跟年輕時的父母，一家人笑得很開心。
->END

== parent_door ==
~ speaker = "我"
「房間門緊鎖，似乎需要一把鑰匙，在手碰到門把的時候，有一股不明的壓迫感從門後傳來，似乎在警告什麼。」
+ 使用鑰匙開門
    {have_items == "key_parent":
        -> have_parent_key
    - else:
        -> no_parent_key 
    }
+ 等等
->END

== have_parent_key ==
~ UnlockDoor("parent_room")
~ Unlock_door = true
~ speaker = ""
【使用道具：父母房間的鑰匙】
使用鑰匙打開了門
門緩緩打開
#FightEnemy
~ speaker = "鬱的化身"
「你...違反規則了」
# jump_to_battle
->END


== no_parent_key ==
~ speaker = "我"
「要先找到鑰匙...」
->END






== parent_room
~ speaker = "我"
「這些是......」
~ speaker = "引路人"
「這裡就是爸爸跟媽媽的房間，你應該很久沒進來了吧。」
~ speaker = "我"
「是啊...我記得以前我都是跟爸爸媽媽一起睡的，但自從爺爺離開家後，我就搬到樓上一個人睡了...」
~ speaker = "引路人"
「再之後你就被限制『不能擅闖父母的房間』，所以也沒機會再進來了」
~ speaker = "我"
「總感覺...這裡好像變了，卻又好像什麼都沒變......」
~ speaker = "引路人"
「房間沒變，但...人心卻變了......」
~ speaker = "我"
「......」
「我...可以問你一個問題嗎...？」
~ speaker = "引路人"
「請問。」
~ speaker = "我"
「剛剛...那個人...他說的都是真的嗎？他...也就是我...殺了自己的爸爸跟媽媽...」
~ speaker = "引路人"
「......」
~ speaker = "我"
「這是真的嗎？」
~ speaker = "引路人"
「起初...你只是帶著半信半疑的想法去到學校後山許願...而那間陰廟也跟學校裡的傳聞一樣，任何願望都能實現」
「於是你去許了第一個願望『希望自己能夠不再被爸媽還有同學們討厭』，很快這個願望就實現了...」
~ speaker = "我"
「真的？可是...為什麼後來變成這個樣子？」
~ speaker = "引路人"
「陰廟裡的那個 “祂” 非常靈驗，“祂” 讓你回到了五年前爺爺還沒離開家之前，一切都還沒改變、還是那麼幸福又快樂的時候，當時的你也沉浸在願望實現的驚嘆跟開心當中。」
「只是好景不常，你雖然回到了過去，卻始終沒能改變爺爺離開家這件事，看著重新獲得的東西再次失去，你比以前都還要痛心百倍...」
「而之後的生活更是如煉獄一般，你不但沒有過上更好的生活，反而比以前都還要慘，可能是心理作用，也有可能是心有不甘，所以後來...」
~ speaker = "我"
「後來...我該不會又去許願了吧？」
~ speaker = "引路人"
「沒錯，你又回到了那個熟悉的地方，再次跟廟裡的那個 “祂” 許願，這一次你打算改變這一切，打算阻止這些本不應該發生的事...」
~ speaker = "我"
「那後來呢？」
~ speaker = "引路人"
「後來...想必你也猜得到，命運是沒辦法改變的...不論你做得再多、再辛苦，最終的結局都還是一個樣，永遠無法改變...」
「而這一次次的輪迴中，你也誕生出了許多的 “角色”，鬱是在你不斷重複體驗著爸媽威壓之下所誕生出來的，而躁則是在你對世界充滿絕望與怨恨時衍生出來的」
「每次輪迴都會因為遇到的人、發生的事、得到的東西有所不同，而有不同的變化，同時這些也會影響 “你” 是個怎麼樣的人」
~ speaker = "我"
「影響...我...是個怎麼樣的人？」
~ speaker = "引路人"
「是的，也就是說你一直不斷重複輪迴到以前你想改變的過去，而這中間經歷的所有人事物都會讓你變成另一個你，換句話說就是你正在經歷的是好幾種不同的時空，每個不同的時間線都有不同的你，而這也會讓你越來越迷失自我」
「至於躁他剛剛說的話，只存在在他那個時空當中，並不影響現在這個時間線的你，你的時間至始至終都還停留在你第一次去許願的那一刻」
~ speaker = "我"
「也就是說...在他的那條平行時空裡...他殺了爸爸跟媽媽嗎？」
~ speaker = "引路人"
「對，他在新的一個輪迴前就已經下定決心，既然他無法改變這一切，那他就用最簡單粗暴的方式來解決，因此他一回到過去便開始失控爆走、大殺四方。」
「可當他解決這一切後又發現這根本不是他想要的，於是後來...他便再次去向那個陰廟許願，繼續開始新的輪迴......」
~ speaker = "我"
「那你...又是......」
~ speaker = "引路人"
「......」
「我是你最一開始的樣子，也就是你最原始的自我」
「你會來到這裡也是因為在這麼多次的輪迴當中你已經漸漸失去了自我，才進入到這個潛意識空間裡面，目的是為了找回迷失的自己」
「不過同時其他平行時空的你也會同時出現來影響你的選擇，必要時就會像剛剛那樣阻止你繼續前進」
~ speaker = "我"
「......」
~ speaker = "引路人"
「資訊太多無法消化嗎...那先好好探索吧...這裡一定可以找到你想知道的一切」
~ speaker = "我"
「好...」
->END

== wardrobe2
~ speaker = "我"
「爸媽的衣櫃從以前到現在都很亂...」
「不知道有什麼藏些什麼東西...」
~ speaker = ""
（伸手到衣櫃裡的各個角落翻找）
~ speaker = "我"
「嗯？這是什麼？」
~ speaker = ""
（在衣櫃的最深處找到了一個透明的夾鏈袋，裡面放了少許的零錢）
~ speaker = "我"
「衣櫃裡怎麼會有這種東西？」
「到底是誰藏的呢？而且為什麼要藏在這裡？」
「重點是感覺錢已經花得差不多了，只剩一點零頭而已」
~ parent_explore += 1
->END

== aunt_letter
~ speaker = ""
一張擺滿化妝品的木桌
~ speaker = "我"
「小時候媽媽都會在這裡化妝，看來現在也還是一樣」
「嗯？抽屜裡好像有什麼東西」
->END

== aunt_letter_end
~ speaker = "我"
「這些是...阿姨寄給媽媽的信？」
「難不成媽媽這些年來一直遭受爸爸的家暴？」
「媽媽...對我的成績會那麼看重，應該也是因為不想讓我變得跟爸爸一樣吧？」
~ speaker = ""
在抽屜的最裡面放著一張紙
~ Get_Clue("Agreement")
~ speaker = ""
......
~ speaker = "我"
「離婚？難道爸爸跟媽媽要離婚？這件事我怎麼不知道？」
~ speaker = "引路人"
「這張離婚協議書還沒有簽字，應該是媽媽事先準備好，但還沒有勇氣拿出來吧？」
~ speaker = "我"
「離婚嗎...不曉得對媽媽來說是不是好事...」
「而且...要是真的離婚了，那我以後該跟誰一起生活呢？日子會不會過得更慘？」
~ speaker = "引路人"
「或許這也是媽媽她有所顧慮的地方」
~ speaker = ""
~ parent_explore += 1
【獲得線索：阿姨給媽媽的信件】
【獲得線索：離婚協議書】
->END

== mom_letter
~ speaker = "我"
「這個櫃子...我記得平常都是放媽媽的東西...」
~ speaker = ""
（打開櫃子翻找了一下，又發現了另外幾封信件、一本銀行存摺，還有一個已經空了的錢包）
->END

== mom_letter_end
~ speaker = "我"
「媽媽...」
~ speaker = ""
（隨後翻了翻在床頭櫃裡找到的存摺跟錢包）
~ speaker = "我"
「這存摺裡的錢長年不到五位數，就連這個錢包也是空空如也...」
「看樣子應該是都被爸爸偷拿去賭博了」
~ speaker = "引路人"
「不曉得你還記不記得家規裡有項規定是 “非正餐時刻不准進食”」
~ speaker = "我"
「記得...以前的我根本不知道設這項規定到底有什麼意義...現在看來，應該是為了節省食材吧？畢竟...我們家的經濟壓力比我想像中還大...」
~ speaker = "引路人"
「而且不定時吃東西會讓胃的作息亂掉，會變得很容易餓或暴飲暴食，睡覺前吃也很容易消化不良，媽媽或許也是為了我們的身體著想才會這麼嚴格」
~ speaker = "我"
「......」
~ speaker = ""
~ parent_explore += 1
【獲得線索：媽媽給阿姨的信件】
->END

== parent_bed
~ speaker = ""
爸媽的床，棉被上有玫瑰花紋圖案
~ speaker = "我"
「爸媽平常在睡覺的床」
「床底下好像藏著什麼...」
（墨涅從爸媽的床底下翻到一張保單）
「這...這是生命保險！？」
「被保險人是爺爺，而保險的受益人是爸爸...」
「而且還是從五年前就開始了...」
「好像跟在保險箱中找到的那個信封內容也有關係，看樣子爸爸跟爺爺之間的感情果然很不好」
~ parent_explore += 1
->END



== bedside_table
~ speaker = "我"
「這個櫃子...我記得平常都是放爸爸的東西...」
~ speaker = ""
（打開櫃子翻找了一下，找到了幾包空的香菸盒還有許多空的酒瓶，也有未拆封過的或還沒用完的）
~ speaker = "我"
「天啊...這麼多的酒瓶，爸爸好像又開始酗酒了」
「而且我怎麼記得爸爸是不抽菸的，他究竟是什麼時候染上菸癮的？」
~ speaker = "引路人"
「可能是創業失利之後吧？那之後他開始借酒澆愁，又靠賭博維生，時間長了壓力也大，自然也容易染上這些壞習慣，但也不排除是他的那些賭友帶壞他的」
~ speaker = "我"
~ parent_explore += 1
「......」
->END

== parent_npc
~ speaker = "引路人"
探索完了嗎?
~ speaker = "我"
.....
+ 好了
    {parent_explore >= 5:
        -> explore_end
    - else:
        -> no_explore 
    }
+ 還沒
    ~ speaker = "引路人"
    再多看看吧
->END

== explore_end
~ speaker = "引路人"
「這樣這間房間就全部探索完了，想必你也已經找到了一切你想了解的真相」
~ speaker = "我"
「其實...我現在並不曉得我該以什麼樣的心情去看待這件事...」
「自從爸爸創業失敗又因為貸款的關係害得我們家負債累累後，我們的生活狀況便開始每況愈下，後來他又到處去賭博，把家裡所有值錢的東西都拿走，最後家裡幾乎什麼都不剩，媽媽只能一直去跟阿姨借錢、兼顧做很多不同的臨時工才能勉強維持家計」
「而媽媽也因為不想看到我也步上爸爸的後塵，所以開始對我的學業要求十分嚴格，寧願犧牲自己所有的休息時間多做幾份工作，也要多賺點錢讓我能夠去補習、加強課業」
「可是...她從來沒有想過我到底喜不喜歡讀書，她只在乎我的成績跟學歷，老是跟我講說『我這麼做都是為你好』，但...真的是為我好嗎......」
~ speaker = "引路人"
「這就要看你怎麼想了，但我認為或許媽媽一開始的出發點是好的，你看看那張寫著家規的字條，很多都是為了你而定的」
~ speaker = "我"
「為了我而定的？」
~ speaker = "引路人"
「那條寫著『不可以擅闖父母房間』的規則，其實是因為她不想讓你看到她被爸爸家暴的樣子。她寧願你誤會，也不願讓你背負那種恐懼。平常她的尖酸刻薄，其實只是用來掩飾她的脆弱和辛勞...」
「而『不可以帶動物回家』，是因為你對動物的毛髮過敏，就像那封信裡講的，你曾經因此進過醫院，她只是怕你身體出問題。」
「『不可以在家喧嘩』、『不可以說髒話』其實也都是為了保護你，怕你引起那個情緒不穩的爸爸的怒氣...」
~ speaker = "我"
「可是...她後來卻也成了我的枷鎖。我就像一個戲中的傀儡，所有的行動、所有的選擇，都被那個操偶師控制著，沒有真正屬於自己的意志。」
「我能理解她這些年的不容易。那些事情，我以前從來都不知道...一直到今天，我才明白她的辛苦與努力。」
「但...她在我心裡留下的傷痕，也不會因此消失。」
~ speaker = "引路人"
「是啊...她的做法，到底是對還是錯...誰也說不準吧。」
~ speaker = "我"
「既然我已經想起了一切，也找到了真相...那我是不是可以離開這裡了？」
~ speaker = "引路人"
「可以。大門的鑰匙就在我這裡，我們離開這裡。」
~ gate_key = true
->END

== no_explore 
~ speaker = "引路人"
似乎還缺少某些東西，再多看看吧
->END

== gate
{gate_key == true:
    ->Home_end
- else:
    ->gate_lock
}
    -
->END

== gate_lock
大門緊鎖著
從窗戶看出去白茫茫一片，像是一層厚重的白霧
->END

== gate_npc
~ speaker = "引路人"
「去吧，從這裡出去，你就可以回到現實世界了」
->END

== Home_end
#gate_NPC
#turn_up
~ speaker = "引路人"
「去吧，從這裡出去，你就可以回到現實世界了」
~ speaker = "我"
「你...不跟我一起走嗎？」
~ speaker = "引路人"
「不要忘了，你就是我，這個地方是你的潛意識空間，你離開這裡，也相當於離開了自己的潛意識空間，回到了現實恢復意識，而我...包括其他不同的 “我” 是沒辦法離開裡的，因為我們是在這個空間裡孕育而生的，至始至終都存在在這裡，所以我們沒辦法離開」
~ speaker = "我"
「那...你要保重」
~ speaker = "引路人"
「快走吧，我也已經達成了我的使命，我很快就會像鬱跟躁一樣消失了」
~ speaker = "我"
「謝謝你...另一個我」
~ speaker = "引路人"
「不用客氣，這是我應該做的」
#turn_back
~ speaker = ""
推開大門，走了出去
#go_out
......
~ speaker = "我"
「怎麼回事？」
~ speaker = "引路人"
「奇怪...不應該這樣才對...難道是哪個步驟出錯了嗎？」
~ speaker = "引路人"
「難道...」
~ speaker = "???"
「嘻嘻～愚蠢！真是愚蠢～」
~ speaker = "我"
「誰？誰在說話？」
~ speaker = "???"
「你真當以為你能夠逃離我的控制嗎？這只不過是一齣我導演的戲劇而已～」
~ speaker = "我"
「什麼......？」
~ speaker = "???"
「想知道這一切的真相嗎？那就再到學校的後山來找我吧～」
~ speaker = "我"
「學校後山...？」
~ speaker = "引路人"
「看樣子是 “它” 搞的鬼」
~ speaker = "我"
「該不會...這所有的一切都是它造成的吧？」
~ speaker = "引路人"
「有可能...或許...從我們去跟它許願的那一刻起，我們就被它控制了」
~ speaker = "我"
「怎麼會......」
~ speaker = "引路人"
「走吧，是時候該為這一切做個了斷了」
~ speaker = "我"
「好！」
->DONE


