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
VAR canHide = false
VAR CANincense = 0
VAR Get_incense = "incense"
VAR inc_interact = 0

EXTERNAL UnlockDoor(door_id)
EXTERNAL SaveGame()
EXTERNAL ChangeBedImage(bed_type)
EXTERNAL ChangeToiletImage(toilet_type)
EXTERNAL MovePlayer(bed_side)
EXTERNAL SpawnObject(chest)
EXTERNAL OpenChestUI()
EXTERNAL SpawnNPC(Guide) 
EXTERNAL HP_Add(hp)
EXTERNAL Get_Item(itemID)
EXTERNAL Get_Clue(clueID)
EXTERNAL ReplaceItem(oldItemID,newItemID)


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
{canHide == true:
    -> hide1
- else:
    -> no_hide1
    }
-> END


== hide1
#Hide1
->END

== no_hide1
~ speaker = " "
裡面放著些許衣服，大多是長袖長褲
~ speaker = "我"
「衣服好少，而且怎麼沒有幾件短袖...」
「似乎可以躲進去...但現在沒這個必要」
->END



== mirror ==
~ canHide = true
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
【獲得線索：日記殘頁-2】
-> END

== note ==
~ speaker = " "
一張紙條，上面詳細寫著各種家規
->END

== note_end ==
~ speaker = " "
【獲得道具“家規”】
~ speaker = "我"
「為什麼這裡會有這種東西？不知道觸發規則會怎樣……好像也跟出去無關。」
 ~ SpawnNPC("Guide")
 ~ speaker = " "
一個幽暗身影出現在旁邊
~ speaker = "我"
「你是剛剛的那個......？」
~ speaker = "引路人"
「繼續前進、繼續探尋，找到最真實的你吧」
「若你心中留有疑惑，來找我...或許我可以為你解答」
~ speaker = ""
【現在開始可以向AI引路人問問題（他只會回答遊戲相關的問題）或許他知道一些重要的線索】
->END


== bed ==
~ speaker = "我"
~ bed_interact += 1
// 檢查是否超過互動次數
{ bed_interact > 2:
    「好像沒有需要調查的地方了」
    -> END
- else:
    * 查看被子
        ~ speaker = " "
        ~ ChangeBedImage("bed_quilt")
        床上凌亂的被子，留有些許溫度
        -> quilt
    * 查看床底下
        ~ MovePlayer("bed_side")
        ~ SpawnObject("chest")
        床底下藏著箱子，將箱子拿了出來，上面有一個4位數密碼鎖
        ~ speaker = "我"
        「鎖住了...密碼是多少呢...是某個日期嗎」
        -> END
}  

== quilt ==
~ speaker = "我"
* 整理被子
    #no_foul
    ~ ChangeBedImage("bed_neat")
    ~ foul = false
    「不整理的話...總感覺會有不好的事發生」
    ->END
* 放著不動
    #foul
    ~ HP_Add(1)
    ~ foul = true
    「還是放著不動吧...應該...會沒事吧」
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
【獲得道具：房間鑰匙】
【獲得線索：墨涅的繪本、一份病歷、幾張藝術比賽的獎狀】
【獲得記憶碎片1/6：不受待見的榮譽】
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
~ speaker = "???"
「你……不是個乖孩子。這樣……會讓你受傷的。」
#Enemy_disappear
~ speaker = "我"
「剛剛那是......?」
「......」
「觸犯規則果然會有危險的事嗎...」
「那聲音……像是我腦海裡最嚴厲的部分，在審判我……」
->END

== no_offense ==
~ speaker = "???"
「你……有聽話。聽話……才不會被傷害。」
#Enemy_disappear
~ speaker = "我"
「剛剛那是......?」
「......」
「觸犯規則果然會有危險的事嗎...」
「但他的語氣……不像是在威脅，反而像是在保證我安全。可這種安全……是不是意味著要放棄什麼？」
「......」
「我為什麼會這麼害怕……是因為我不想被懲罰，還是因為我怕失去他們的認同？」
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

== glass_cabinet
~ speaker = " "
櫥櫃緊鎖著，裡面放著一幅全家福，是小時候的墨涅跟年輕時的父母，一家人笑得很開心。
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
洗手台裡放滿了水，水上面放了一張被水浸濕的信封，還有幾根不知道是甚麼動物的毛
~ speaker = "我"
「為什麼這洗手台上有這麼多的毛啊...？」
~ speaker = " "
用手觸碰了水裡的毛
#memory1
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
        ~ HP_Add(1)
        「......」
        ->refrigerator_memory
    * 不打開冰箱
        ~ speaker = "我"
        「現在肚子不餓，不用找吃的」
        「嗯？冰箱上好像貼著什麼東西」
        「這是...又一個日記殘頁？」
        ~ Get_Clue("Journal3")
        ->Journal3
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
~ speaker = ""
......
~ speaker = "我"
「從那之後...我好像再也沒在半夜跑到廚房了...」
「我一直認為，他們的規則只是為了讓我服從……」
「可那天的媽媽，似乎真的只是怕我累壞……」
「或者，我只是想把每一次的限制都解讀成惡意，這樣我才有理由反抗。」
->END

== Journal3
~ speaker = "我"
「......」
「爸跟媽...常常在吵架？」
「該不會...就是因為他們長期處的不愉快，才把氣全部出在我身上吧...」
「那我到底算什麼...我不是他們的孩子嗎？為什麼我要承擔他們不滿的情緒？他們有尊重過我嗎...」
「在他們眼裡...我究竟是他們的親生骨肉，還是可以隨意任由他們出氣的沙包？」
~ speaker = " "
【獲得線索：日記殘頁-4】
->END

== kitchen_sink
{canHide == true:
    -> hide2
- else:
    -> no_hide2
    }
-> END

== hide2
#Hide2
->END

== no_hide2
~ speaker = " "
水池裡有一點水珠，似乎最近有使用過，底下的櫥櫃很空似乎可以躲藏。
->END


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
->END

==letter_end
~ speaker = "我"
「......貓毛過敏...原來家裡禁止帶動物回家是因為我嗎...」
「那天我只記得自己被罵、被禁足……卻沒想過，也許他們是真的在擔心我」
「如果這是真的……那我一直以來的怨恨，會不會有一部分，是誤會？」
「可是……如果他們是愛我的，為什麼還要用懲罰代替解釋？...他們口中的保護，為什麼要讓我覺得自己像犯了罪？」
「也許……真相，並不會讓我感到好受」
~ speaker = ""
【獲得線索：一封老舊的信件】
【獲得記憶碎片2/6：未曾解釋的擔心】
->END

== knife_holder
~ speaker = ""
上面放著三種不同款式的菜刀
~ speaker = "我"
「奇怪...為什麼我看到這個菜刀會有種想拿的衝動......？」
* 拿起
    #play_cg knifeCG
    「......」
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
~ HP_Add(1)
->END

== cloth_wash
~ speaker = ""
普通的洗衣機
->END

== clothes_basket
~ speaker = ""
衣物堆積在了籃子裡。
~ speaker = "我"
「堆了好多衣服...這到底多久沒清理了啊？」
~ speaker = ""
在衣物堆裡面翻找起來
找到被揉皺的紙團
把紙團打開，裡面掉出一把舊鑰匙
紙團上面寫著「百物歸處，木門為鎖，灰塵作守。」
【獲得道具：不明的鑰匙、揉皺的紙團】
->END

== clothes_basket_end
~ speaker = "我"
「這紙團是指這把鑰匙對應的鎖嗎...」
~ speaker = ""
後面出現了黑影
~ speaker = "引路人"
「你找到了新的線索了啊...」
~ speaker = "我"
* 「Ｘ！你有什麼毛病啊！」
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
    ~ speaker = "???"
    「你...觸犯了規則......」
    他的聲音低沉，像是從我胸腔深處傳出。
    ~ speaker = "???"
    「別去看……別去想……那些記憶只會讓你痛苦。」
    ~ speaker = ""
    這語氣不像威脅，更像勸阻。
    ~ speaker = "我"
    「他在擋我……可為什麼，我從他的語氣中我感覺到了害怕？」
    ~ speaker = "引路人"
    「現在不是說這個的時候，快跑！找個地方躲起來」
    #start_chase
    ~ canHide = true
    ->END
* 「怎麼又是你」
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
    ~ speaker = "引路人"
    「我只是出來提醒你，要謹記你房間裡那張寫著家規的紙條」
    ~ speaker = "我"
    「知道了...」
    語畢，引路人又消失的無影無蹤了
    ~ speaker = "我"
    「真是個奇怪的傢伙。算了！我還是趕快去其他地方找找看有沒有其他線索」
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
「該拿幾支香來拜呢？」
{ inc_interact > 5:
    「不需要再拜拜了」
    -> END
- else:
    ~ speaker = ""
    *【一炷香】
    「如果我想請神明幫忙，只點燃一支香可能不夠......」
    ->END
    *【三炷香】
        #burn
        ~ HP_Add(1)
        ~ speaker = "我"
        「我...好像只求過一次…那是我唯一主動去祈求神明的時候。」
        「......」
        「可那真的是我想要的嗎？還是……只是因為我想滿足他們的期待？」
        「連祈願的方式……都要有人告訴我『正確』的做法。
        可是，神明在意的……真的是香的數量，還是我祈求時的心意？」
        ~ speaker = ""
        （拿起神桌上的打火機點亮了手中的香，拜三拜，向神明祈求，隨後把香插入香爐內）
        #open_forcer
        （角落放著祖先牌位的櫃子發出了咖噠一聲）
        ~ speaker = "我"
        「也許...我早就忘了自己想求什麼，只記得該怎麼做才能讓他們滿意。」
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
}  



== god_forcer
打開櫃子，裡面放著一把金色的鑰匙，上面寫著 「櫥櫃鑰匙」
~ Get_Item("key_gold")
裡面還放著一個平安符，上面寫著「保佑闔家平安」
#memory3
->amulet_memory

== amulet_memory
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
【獲得線索：平安符（可抵擋一次攻擊）】
【獲得記憶碎片6/6：他們的願望】
->END

== cabinet
~ speaker = ""
鞋櫃裡有很多雙鞋
~ speaker = "我"
「有好幾雙鞋子特別髒...」
「嗯？鞋櫃裡好像有幾張紙...」
->END







== parent_room ==
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
->END

== no_parent_key ==
~ speaker = "我"
「要先找到鑰匙...」
->END










