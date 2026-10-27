using System;
using System.Collections.Generic;
namespace Qingming
{
    public partial class DayState
    {
        public class Option { public string id,text; public bool disabled; public Option(string i,string t,bool d=false){id=i;text=t;disabled=d;} }
        public List<Option> Options()
        {
            var a=new List<Option>();
            if(Complete()){a.Add(new Option(chapter==13?"finish":"next",chapter==13?"看这一日的落款":"继续 · "+Chapters[chapter+1]));return a;}
            switch(chapter){
            case 0:
                if(step==0){a.Add(new Option("rope_fix","换好旧绳，试提空篓"));a.Add(new Option("rope_later","先出门，送货前再检查"));}
                if(step==1)a.Add(new Option("promise","记下：十二只完好梨，午后送茶坊"));
                if(step==2)a.Add(new Option("mother","接过八十枚本钱，让母亲歇歇手"));break;
            case 1:
                if(step==0){a.Add(new Option("inspect","翻看篓底，逐只看果皮"));a.Add(new Option("skip_inspect","先收下这批，摆摊时再看"));}
                if(step==1){a.Add(new Option("batch_keep","收二十二好、二轻伤 · 48 枚",coins<48));a.Add(new Option("batch_replace","请果行换掉两只轻伤 · 48 枚",coins<48));a.Add(new Option("batch_discount","说明轻伤，商定整批 46 枚",coins<46));}
                if(step==2)a.Add(new Option("reserve","点出十二只完好梨，放进订货篓"));break;
            case 2:
                if(step==0)a.Add(new Option("place","提起竹篮，靠摊摆放，留出通道"));
                if(step==1){a.Add(new Option("call","开口招呼：梨有好有伤，各自明价"));a.Add(new Option("quiet","摆好价牌，等行人停步"));}
                if(step==2)a.Add(new Option("meet","与顾嫂、老周打个照面"));break;
            case 3:
                if(step==0){a.Add(new Option("gu_good","给三只完好梨 · 收 12 枚",retail<3));a.Add(new Option("gu_honest","说明碰伤：两好一伤 · 收 10 枚",retail<2||bruised<1));a.Add(new Option("gu_hide","不提碰伤：两好一伤按好货收 12 枚",retail<2||bruised<1));}
                if(step==1){a.Add(new Option("watch","约好照看到午后，借一块布巾"));a.Add(new Option("no_watch","自己收拢摊货，也不借布巾"));}
                if(step==2)a.Add(new Option("gu_leave","复述买卖与托看约定，送顾嫂离开"));break;
            case 4:
                if(step==0){a.Add(new Option("money_count","确认按实际枚数：四只好梨共 16 枚",retail<4));a.Add(new Option("money_demo","先看钱陌演示，再按十六枚结算",retail<4));}
                if(step==1){a.Add(new Option("keep_order","这十二只已答应别人，先留着"));a.Add(new Option("ask_order","取得许娘子减量同意后再另售"));a.Add(new Option("notify_order","仅递话后就另售，尚未获同意"));a.Add(new Option("sell_reserved","未通知，先另售零售余梨和三只订货梨"));}
                if(step==2){a.Add(new Option("merchant_route","再问沈客一路的脚钱、渡钱和税费"));a.Add(new Option("merchant_end","核对钱货，准备送货"));}
                if(step==3)a.Add(new Option("watch_notice",watchAgreed?"告知顾嫂送货路线，请她到时收拢摊面":"收拢摊面，把梨随身带上"));break;
            case 5:
                if(step==0){a.Add(new Option("bridge_help","按何四指示移开自己的篓，给船工留路"));a.Add(new Option("bridge_stand","退到安全处，看船员协作"));}
                if(step==1)a.Add(new Option("bridge_pass","看船过桥，再沿岸前行"));break;
            case 6:
                if(step==0){a.Add(new Option("walk","重新检查绳结，自己沿岸走"));a.Add(new Option("porter","先问老周是否得空 · 约定工钱 4 枚"));a.Add(new Option("boat","请何四安排一段渡送 · 3 枚",coins<3));}
                if(step==1){if(porterAgreed){a.Add(new Option("porter_pay","到茶坊付清工钱 · 4 枚",coins<4));a.Add(new Option("porter_later","说明晚间再结工钱"));}else a.Add(new Option("arrive",rodeBoat?"坐稳看岸景，由船夫操舟靠岸":"托稳篮底，沿岸送到茶坊"));}break;
            case 7:
                if(step==0)a.Add(new Option("deliver","打开订货篓，请许娘子逐只验收"));
                if(step==1){if(accepted<order&&!teaFollowup){a.Add(new Option("replenish","先结已收的梨，回市补足",replenishVisits>0));a.Add(new Option("renegotiate","当面请求按已收数量结清"));a.Add(new Option("tomorrow","说明困难，请求明早补齐"));a.Add(new Option("leave_short","只收已交货款，留下未履行的缺口"));}else a.Add(new Option("tea_settled","核对交付数量与实收货款"));}
                if(step==2){a.Add(new Option("tea_try","听示范，试一遍研、罗、温盏、调膏、击拂"));a.Add(new Option("tea_watch","只看许娘子示范，不亲手尝试"));}
                if(step==3){if(teaPractice>0&&teaPractice<7)a.Add(new Option("tea_step",new[]{"","研细茶末","罗筛茶末","温盏","少汤调膏","注汤","击拂并观察"}[teaPractice]));a.Add(new Option("tea_end",teaPractice<7?"结束练习，收好空篓":"收好空篓，谢过许娘子"));}break;
            case 8:
                if(step==0){if(guDispute&&!guResolved){a.Add(new Option("gu_swap","承认没有说清，换一只好梨",retail<1));a.Add(new Option("gu_refund","收回顾嫂三只梨，退还 12 枚",coins<12));a.Add(new Option("gu_ignore","暂不处理，记下未解决争议"));}else a.Add(new Option("revisit","问顾嫂摊位情形，交代送货结果"));}
                if(step==1){if(teaDeferred&&accepted<order){int n=order-accepted;a.Add(new Option("buyback","向沈客买回 "+n+" 只 · 退 "+(n*4)+" 枚",shenOwned<n||coins<n*4));a.Add(new Option("restock","果行另购 "+n+" 只 · "+(n*4)+" 枚",coins<n*4));a.Add(new Option("no_restock","不再补货，保留未履约记录"));}else a.Add(new Option("no_restock","核对茶坊交付记录"));}
                if(step==2){a.Add(new Option("repair","请修篓人加固篓口 · 3 枚",coins<3));a.Add(new Option("no_repair","自行检查，将修补留到明日"));}
                if(step==3){a.Add(new Option("late_sell","再售两只完好梨 · 收 8 枚",retail<2));a.Add(new Option("gift","将一只零售梨送给顾嫂",retail+bruised<1));a.Add(new Option("keep_fruit","把余梨留作家用和明日安排"));}break;
            case 9:
                if(step==0)a.Add(new Option("pack","收起竹篮，清点好梨、轻伤梨和借物"));
                if(step==1){a.Add(new Option("count","再数一遍，先留足明日本钱"));a.Add(new Option("family_funds","将 48 枚交家中日用，经营钱另算",coins<48));}break;
            case 10:
                if(step==0){if(porterDebt>0){a.Add(new Option("debt_pay","先把老周的 4 枚工钱付清",coins<4));a.Add(new Option("debt_keep","说明钱还没付，留下欠账"));}else a.Add(new Option("zhou_talk",porterAgreed?"谢谢老周白日受托，坐下聊聊":"先问老周今天的活计"));}
                if(step==1){a.Add(new Option("buy_wine","要一壶薄酒 · 6 枚",coins<6));a.Add(new Option("drink_tea","以茶代酒 · 2 枚",coins<2));a.Add(new Option("drink_water","只坐着喝水，不添开销"));}
                if(step==2){a.Add(new Option("warm",drink=="酒"?"温碗置热水，以注子温酒":"给同桌递盏，慢慢喝"));}
                if(step==3){if(drink=="酒"&&pour<1){if(!jugReady)a.Add(new Option("pour_wine","拿起注子，按住 E 慢斟（可选）"));a.Add(new Option("pour_auto","辅助斟好一盏，无需持续按键"));}a.Add(new Option("talk_day","把送货的实情告诉老周"));a.Add(new Option("talk_work","不说送货，听老周说今日的工钱"));}break;
            case 11:
                if(step==0){a.Add(new Option("show_pay","按本棚约定入座 · 2 枚",coins<2));a.Add(new Option("show_outside","在允许停留的棚外看节目"));}
                if(step==1){a.Add(new Option("axiu_tell","演前告诉阿绣今天送梨的事"));a.Add(new Option("axiu_quiet","只问节目名字，不谈私事"));}
                if(step==2)a.Add(new Option("show_open","听《一担梨》开场"));
                if(step==3){a.Add(new Option("answer_keep","接话：先留着"));a.Add(new Option("answer_ask","接话：先问一声"));a.Add(new Option("answer_silent","安静听，让搭档接下去"));}
                if(step==4)a.Add(new Option("show_end","听转段与收尾"));
                if(step==5){a.Add(new Option("backstage","演后扶住道具篓，配合一次出入场"));a.Add(new Option("no_backstage","向阿绣道别，准备回家"));}
                if(step==6)a.Add(new Option("leave_show","带着记住的那一句，走出瓦舍"));break;
            case 12:
                if(step==0)a.Add(new Option("night_lane","听铺兵指路，让出取水与取具通道"));
                if(step==1){a.Add(new Option("return_cloth",cloth&&!clothReturned?"还顾嫂布巾，再确认白日争议":"向顾嫂道晚安"));if(cloth&&!clothReturned)a.Add(new Option("keep_cloth","今夜先不归还，把借物记在账上"));}break;
            case 13:
                if(step==0)a.Add(new Option("home_rest","把篓放进门，先坐下让肩膀歇一会儿"));
                if(step==1)a.Add(new Option("home_account","向母亲点交钱货，说明未完约定"));break;
            }
            return a;
        }
        public bool Apply(string id)
        {
            if(finished)return false;
            var option=Options().Find(x=>x.id==id&&!x.disabled);if(option==null)return false;
            dialogue.Add(Speaker()+"\n"+(receipt.Length>0?receipt+"\n":"")+Line()+"\n阿安："+option.text);receipt="";
            if(id=="pour_wine"){jugReady=true;receipt="已拿起注子。按住 E 或画面中的鼠标左键慢斟；也可选辅助斟好，或放下继续说话。";return true;}
            if(id=="pour_auto"){pour=1;receipt="一盏已斟好。斟多少不影响人物关系；不把进度条当作宋代礼法。";return true;}
            if(id=="tea_step"){receipt=new[]{"","茶末研细。许娘子提醒：先看粗细，不必求快。","筛去粗粒，让末更匀。","先温盏，操作由熟手照应。","少汤入末，先调成匀膏。","沿盏注汤，量与次序要照应。","击拂后观察汤花。许娘子：还需练，今日识得次序就好。"}[teaPractice];teaPractice++;return true;}
            if(id=="next"){Next();return true;}if(id=="finish"){finished=true;return true;}
            switch(id){
            case "rope_fix":ropeFixed=true;receipt="换好家中备用绳，提空篓试了试。";break;
            case "promise":Memory("母亲托我送十二只完好梨；她的手臂今日要歇歇");break;
            case "inspect":inspected=true;receipt="翻到底层：二十二只完好，两只轻微碰伤，并非腐坏。";break;
            case "batch_keep":Buy(22,2,48,"果行整批进货");break;
            case "batch_replace":Buy(24,0,48,"果行换好后进货");receipt="果行收回轻伤果，换入两只好梨，总数仍是二十四只。";break;
            case "batch_discount":Buy(22,2,46,"果行议定轻伤整批价");discountBatch=true;break;
            case "reserve":Memory("十二只好梨另篓预留，零售与订货分清");break;
            case "gu_good":Sell(3,0,12,"顾嫂");guGood=3;guPaid=12;break;
            case "gu_honest":Sell(2,1,10,"顾嫂（已告知轻伤）");guGood=2;guBruised=1;guPaid=10;receipt="顾嫂：我今早就吃。价与成色说清，我才好挑。";break;
            case "gu_hide":Sell(2,1,12,"顾嫂（未说明轻伤）");guGood=2;guBruised=1;guPaid=12;guDispute=true;hadBreach=true;break;
            case "watch":watchAgreed=true;cloth=true;receipt="顾嫂答应照看到午后；到时会收拢摊面。布巾要还。";break;
            case "money_count":case "money_demo":Sell(4,0,16,"沈客");shenOwned=4;receipt=id=="money_demo"?"钱陌教学的示意铜钱已经收回，不入钱袋。双方另数十六枚，结清这四只梨。各行市钱陌有别，不是通用折扣。":"双方当面数清十六枚，四只好梨交给沈客。";break;
            case "ask_order":consent=true;order=9;receipt="许娘子当面答复：今日可改为九只好梨，按实收结算。你回来后才答应沈客。";SellExtra();break;
            case "notify_order":notified=true;receipt="传话人尚未带回答复。你仍先另售了三只订货梨；原约十二只仍有效。";hadBreach=true;SellExtra();break;
            case "sell_reserved":hadBreach=true;receipt="没有得到茶坊同意，原约仍是十二只。货款已收，少的三只需要真实补回。";SellExtra();break;
            case "merchant_route":receipt="沈客：雇脚、渡运、吃住各算各的；税也看货物与当地办法。莫拿一条税率包了所有商旅。";Memory("听沈客说路：行旅成本并非一个统一税率");break;
            case "watch_notice":watchNotified=watchAgreed;break;
            case "bridge_help":bridge_helped=true;Memory("依何四指示让路，船员自有分工");break;
            case "bridge_pass":bridge_watched=true;break;
            case "walk":route="沿岸步行";ropeFixed=true;break;
            case "porter":route="老周受托帮运";porterAgreed=true;porterDebt=4;ropeFixed=true;receipt="老周：这趟活已交割，我可帮这一程。四枚工钱，到茶坊算。";break;
            case "boat":Pay(3,"短程渡送");route="何四安排渡送";rodeBoat=true;ropeFixed=true;receipt="何四安排好坐处，交代不要碰缆。船由熟手操持。";break;
            case "porter_pay":case "debt_pay":Pay(porterDebt,"老周搬运工钱");porterDebt=0;porterPaid=true;break;
            case "deliver":int n=Math.Min(good,order-accepted);Sell(n,0,n*3,"许娘子验收");accepted+=n;receipt="逐只验收完好梨 "+n+" 只，实收 "+(n*3)+" 枚。轻伤梨留在阿安篓中，未冒充交付。";break;
            case "replenish":teaDeferred=true;receipt="许娘子：已交的先结清，缺的回市找；若找不到，回来把话说明白。";break;
            case "renegotiate":consent=true;order=accepted;teaClosed=true;receipt="许娘子：今日先按这些结清。下次改数，要先等我答复。双方这才同意变更。";break;
            case "tomorrow":teaFollowup=true;receipt="许娘子明确同意：剩余好梨明早送来。明日仍有 "+(order-accepted)+" 只待交，今日不预收款。";break;
            case "tea_try":teaPractice=1;receipt="许娘子逐步指点。你可继续按键练习，不以手快或茶沫判定品行。";break;
            case "tea_watch":teaPractice=7;receipt="先听懂次序。研罗、温盏、调膏，再注汤击拂；这是对文献的简化演示。";break;
            case "gu_swap":good--;bruised++;guGood++;guBruised--;guResolved=true;receipt="顾嫂交回轻伤梨，阿安换给一只好梨；两边各换一只，钱款不变。";break;
            case "gu_refund":Pay(guPaid,"退顾嫂货款");good+=guGood;bruised+=guBruised;sold-=guGood+guBruised;guGood=guBruised=0;guResolved=true;receipt="三只梨真正收回，十二枚真正退还，争议结清。";break;
            case "buyback":int b=order-accepted;Pay(b*4,"退沈客货款，回购好梨");shenOwned-=b;good+=b;sold-=b;BeginReturn();return true;
            case "restock":int k=order-accepted;Buy(k,0,k*4,"果行补购");BeginReturn();return true;
            case "no_restock":teaDeferred=false;break;
            case "repair":Pay(3,"修篓人工钱");repairDone=true;Memory("修篓人收三枚加固篓口：手艺也须算工");break;
            case "late_sell":Sell(2,0,8,"收摊前零售");lateSold=true;break;
            case "gift":if(bruised>0){bruised--;receipt="说明轻伤后赠出，顾嫂收下作今晚吃用。";}else{good--;receipt="赠出一只零售好梨，不动订货篓。";}gifts++;ledger.Add("赠顾嫂一只梨，库存 −1，无收入");break;
            case "pack":packed=true;break;
            case "count":counted=true;Memory("钱袋数的是实物枚数；余钱增加不等于利润");break;
            case "family_funds":Pay(48,"拨付家中日用（资金转出，非消费价格）");familyFunds=48;counted=true;receipt="四十八枚转作家庭日用，已单列，不计为经营余钱；若明日本钱不足，要另作安排。";Memory("家用与经营钱分开，生活需要也是真实的安排");break;
            case "buy_wine":Pay(6,"小酒肆薄酒");drink="酒";paid_wine=true;break;
            case "drink_tea":Pay(2,"小酒肆茶水");drink="茶";break;
            case "drink_water":drink="水";break;
            case "warm":pour=drink=="酒"?0:1;break;
            case "talk_day":toasted=true;receipt="老周听完才知道送货经过：能补就补，有话明日当面说；别把别人的空当都算成自己的。";Memory("把送货实情告诉老周");break;
            case "talk_work":toasted=true;receipt="老周只说自己知道的活计：等货、扛包、交割，做完才领钱。";break;
            case "show_pay":Pay(2,"本棚看座");theatre_paid=true;break;
            case "axiu_tell":axiuKnows=true;receipt="阿绣听你说完交付经过：这戏原就唱买卖人，你听见熟的，别急着认自己。";break;
            case "answer_keep":audience="先留着";receipt="搭档：留到什么时候？\n阿绣：留到说好的时候。过了时候，再去问。";break;
            case "answer_ask":audience="先问一声";receipt="阿绣：这主意好。可问的人也要走路，不能嘴上一问，手里先卖了。";break;
            case "answer_silent":audience="安静听";receipt="搭档自己问：可要先问一声？节目照常接下去。";break;
            case "backstage":backstage=true;Memory("扶道具篓配合出入场，台下也是人的工作");break;
            case "return_cloth":clothReturned=cloth;receipt=cloth?"布巾已还。"+(guDispute&&!guResolved?"顾嫂提醒：梨的事情仍没有解决。":"顾嫂：明日见，有事当面说。"):"顾嫂收摊了，向你道一声晚安。";break;
            case "home_account":Memory("归家向母亲点交钱货，不把未完的事藏在好听话里");break;
            }
            step++;return true;
        }
        void SellExtra(){int n=good-9;Sell(n,0,n*4,"沈客增购（含原订货三只）");shenOwned+=n;diverted=true;receipt+=" 实际增售 "+n+" 只、收 "+n*4+" 枚；篓中剩九只好梨。";}
        void BeginReturn(){replenishVisits++;teaDeferred=false;chapter=7;step=0;receipt="已取得真实补货，回茶坊二次验收。此补货回路只开放一次。";}
        public string Speaker(){return new[]{"阿安 · 安母","果行卖者 · 阿安","顾嫂 · 老周","顾嫂","沈客","何四 · 船工","老周 · 何四","许娘子","顾嫂 · 沈客 · 修篓人","阿安","老周","阿绣 · 搭档","铺兵 · 顾嫂","安母 · 阿安"}[chapter];}
        public string Line()
        {
            if(Complete())return chapter==13?"安母听完，先把明日要做的事一件件排开。\n"+Obligations():"这一段的事已记在账上。\n"+(chapter==7?Obligations():chapter==9?"钱货要对得上，尚未履行的约定也要带着走。":"接着走阿安的一天；账本可随时回看对话与收支。");
            switch(chapter){
            case 0:return new[]{"母亲揉了揉胳膊，把空篓推近。\n安母：绳磨毛了，今日别只顾着赶早。\n阿安：我想自己走完这一趟。","安母：许娘子昨日定了十二只，要完好的，午后送到。先答应人的那份，得记牢。\n阿安：另篓放，不拿去混卖。","安母：八十枚是本钱。卖得如何，回来一起数。\n阿安：您歇歇手，篓我来提。"}[step];
            case 1:return new[]{"天刚亮，果行已经有人卸货。上层的梨齐整，篓底却还看不清。\n卖者：一共二十四只，别只瞧上面。",""+(inspected?"阿安翻出了两只轻伤梨。":"摆开后才发现底下两只轻伤梨。")+"卖者：可以换，也可以把成色与整批价说清。\n轻伤可说明后零售，不拿它充茶坊要的好梨。","把十二只完好梨另放。其余才是零售；预留仍属于阿安，但已有不能单方更改的约定。"}[step];
            case 2:return new[]{"摊边有挑担人经过，阿安先挪自己的篓。留出路，别让叫卖成了拦路。","阿安试着把声音送到行人耳边。\n顾嫂：你说梨，还是跟自己的鞋说话？把成色说清就好。","老周扛着货包停了一下：我这趟还没交割，有事先问，莫当我总有空。\n阿安应了声，顾嫂已经在摊前挑梨。"}[step];
            case 3:return new[]{"顾嫂：家里今日吃，三只便够。别替我作主，把好的、碰伤的和价都说来。\n阿安手里有轻伤果，要怎样交这笔货？","顾嫂：我可替你照看到午后，之后我也有自己的活。布巾借你垫篓，晚间还我。\n照看不是替阿安全天卖货，也不改变梨的归属。","顾嫂把自己的梨装好。\n阿安复述照看时间与借物，免得一句“帮我看看”变成谁也说不清的托付。"}[step];
            case 4:return new[]{"沈客：四只好梨带路上。你这里说的钱，是怎样数？\n阿安：这一笔，四只共十六枚实物铜钱，当面数清。\n钱陌演示另用示意物，不进收入。","沈客又看向篓里：剩下零售的我也可买，再添你留着的三只？\n那三只已有茶坊之约。若另售，这次实际增购 "+Math.Max(0,good-9)+" 只，篓中只留九只好梨。","沈客：行路不止买货一笔钱，脚夫也有脚夫的账。\n他只知道眼前这笔买卖，不会知道茶坊是否接受了你的安排。","出发前再检查预留与钱袋。\n"+Obligations()}[step];
            case 5:return new[]{"桥口人群挤近，何四指向岸边：先把你自己的篓移开，这里要过人！\n船上的熟手各守位置，压低桅杆、操持船行。阿安只做听清并能做到的事。","船向桥洞靠近。何四盯着船岸呼应。\n"+(bridge_helped?"你让出的通道已空出来，何四看见，点头示意。":"你站在安全处，船员仍照常完成自己的工作。")}[step];
            case 6:return new[]{"该把梨送到茶坊。水上有人操舟，岸上有人凭肩背吃饭，哪一条路都不是白得的。\n旧绳若尚未处理，此刻先收紧加固再行。",""+(porterAgreed?"老周已经同意这趟帮运：说好的四枚，到这里算。":rodeBoat?"何四：坐稳，别拿船上的缆当扶手。靠岸后我来安排下船。":"阿安托住篓底沿岸走，远处叫卖声一阵近、一阵远。")}[step];
            case 7:return new[]{"许娘子先看篓，再对订单：原说十二只好梨，咱们按真正收到的数。\n"+(notified&&!consent?"我收到传话了，可没答应减量。通知和改约是两件事。":consent?"先前双方说定的新数量也已记下。":"先别忙着解释，一只只摆开看。"),"已验收 "+accepted+" 只；现约 "+order+" 只。\n"+(accepted<order?"许娘子：少的要怎么安排，你当面说明，我也要决定今日如何用。":"许娘子：数与成色都对了，这一笔结清。"),"许娘子将茶末研细、筛罗，先温盏，少汤调膏，再注汤击拂。\n阿安：看着快，做起来手就乱。\n许娘子：这是日日做的工夫。今日先把次序看明白。","许娘子：茶不是沫子越高越好，更不是拿来判一个人好坏的。\n你收篓准备回市。"}[step];
            case 8:return new[]{guDispute&&!guResolved?"顾嫂把梨摆在眼前：我买时你没提这处伤，钱却按好货收。你打算怎样办？\n更换要有真实零售好梨，退款要真正收回原货。":"顾嫂：你回来了。"+(watchAgreed?"到了约好的时候，我便把摊面收拢。":"我自己的事也才忙完。")+"\n她只问你说出来的送货经过。",teaDeferred?"茶坊还缺 "+(order-accepted)+" 只好梨。沈客尚未离开，自己买下的梨仍在手里；果行也还有一小批。\n买回要退款，另购要花钱，不能把别人的梨直接拿走。":"回访时再核对一次，已交的不能再收第二次钱。\n"+Obligations(),"修篓人捏住篓口：这处先加固，别等装满才散。三枚，愿意就坐会儿。\n手艺有工钱，阿安可以修，也可以明日再安排。","还有余梨。先留好仍需交付的部分，零售、赠出或带回家，去向都要入账。"}[step];
            case 9:return new[]{"暮色到了。空篓并不等于好生意，剩货也不等于没做成事。\n先数现存：完好 "+good+" 只，轻伤 "+bruised+" 只；再看借来的布巾。","钱袋 "+coins+" 枚。累计收款 "+revenue+" 枚，支出及退款 "+expenses+" 枚。\n明日按演示设定至少留四十八枚进货本钱。现金差额不是利润，余货和未完约定还在。"}[step];
            case 10:return new[]{porterDebt>0?"老周坐下先说：今日帮你那程，说好的四枚还没结。酒是酒，工钱是工钱。":"老周：我今日也忙到这会儿。坐，先歇歇脚。\n"+(porterPaid?"白日搬运的工钱已两清。":"阿安不把朋友的劳动当作理所当然。"),"小酒肆里有酒，也能喝茶或只歇脚。\n这家是故事里的小酒肆；画上的孙羊正店招牌只作观画线索。\n不喝酒也能把话说完。",drink=="酒"?"把热水置于温碗，以注子温酒，再慢慢斟。\n这一操作是简化展示；不把斟满比例或碰杯当成宋人统一礼法。":"阿安递过盏：今晚这样坐会儿也好。\n老周：喝什么不打紧，把腿歇过来。",""+(bridge_helped?"何四路过，只说桥口那次让路他看见了，别的事他并不知道。\n":"")+"老周：今日送梨，还顺当么？\n你可以讲实情，也可先听他的工作。"}[step];
            case 11:return new[]{"阿绣在棚边理道具：来听《一担梨》？唱买卖人的。\n本棚虚构约定看座两枚，也准在外侧停留听看；不是宋代统一票价。","阿绣指向空篓：你听见熟的，别急着认自己。\n节目原就这样唱，演出者不会自动知道你今日的私事。","阿绣唱：\n天未明，篓先开，路上行人各自来。\n好的留谁，熟的谁带，一篓未空，几句话先排。\n搭档：你这篓里，怎么一句话也能卖？\n阿绣：梨是今日的，话是昨日答应的。\n搭档：昨日的话，今日可涨价？\n阿绣：你要先去问那听话的人。","搭档绕到篓后：这边也有梨。一起给我？\n阿绣护住篓：这一份有人要。\n搭档：人在哪里？\n阿绣：还没来。\n搭档：人没来，钱倒可以先来。\n阿绣看向观众：听见没有？钱比人脚快。","搭档换作赶路人：桥口挤，街上忙，你这一篓还送不送？\n阿绣：送。\n搭档：若送不到？\n阿绣：去个话。\n搭档：去话不要腿？\n阿绣：要。所以平日别只认人家的手，不认人家的腿。","阿绣唱：\n空篓轻，脚下迟，今日的账，灯下知。\n有钱留作明朝本，有话留到见人时。\n搭档掀篓：一只也没剩？\n阿绣取出道具梨：还有一只。\n搭档：又许给谁？\n阿绣：留给卖梨的人。",axiuKnows?"阿绣：听你先前说的送货经过，明日还有的事便当面说。我们台上这几步，台下也要有人收拾。":"阿安：你怎么知道桥口？\n阿绣：我也要从那边过。又不是只有你出门。\n她没有听说你的送货细节，不会替许娘子下判断。"}[step];
            case 12:return new[]{"夜市还没有全歇。铺兵看着取水、取具的通道，提醒阿安把篓靠里。\n无需一场火灾来证明他们有用，巡守是他们本来的工作。","顾嫂也在收尾。\n"+(cloth&&!clothReturned?"借来的布巾还垫在篓底，要拿出来还给她。":"今日没有未归还的布巾。")+"\n"+(guDispute&&!guResolved?"果品争议仍在，不能用一句晚安抵掉。":"该说的话说清，再各自回家。")}[step];
            case 13:return new[]{"安母没有先问赚了几枚，只把凳子拉近：篓放进来，先坐。\n阿安把绳从肩头卸下，才觉出这一日的分量。","阿安摊开钱袋与余梨：本钱、收款、花费，还有没做完的事，都在这里。\n安母：明日一件件做。\n"+Obligations()}[step];
            default:return "";
            }
        }
    }
}
