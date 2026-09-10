/*
 NPC: 史匹奈亞 Spinel (9000020) — 世界旅行嚮導 World Tour Guide
 所在:弓箭手村/魔法森林/勇士之村/墮落城市/港口/天空之城/冰原雪域/玩具城/神木村
       以及各國際村(日本/台灣/中國/泰國)
 功能(照原廠):
   在家鄉城鎮 → 付費前往 日本古代神社 / 台灣西門町 / 中國上海灘 / 泰國水上市場
   在國際村   → 可跳轉到其他國際村,或免費回到出發的城鎮
 費用:初心者/新手 300 楓幣,其餘 3000 楓幣;國際村之間跳轉每次 3000 楓幣。
*/

var status = -1;
var cost = 3000;
var sel = -1;
var dests = [];
var destMaps = [];

// 四大國際村主城
var JP = 800000000;   // 日本 古代神社
var TW = 740000000;   // 台灣 西門町
var CN = 701000000;   // 中國 上海灘
var TH = 500000000;   // 泰國 水上市場
var HOP = 3000;       // 國際村之間跳轉費用

function isTourMap(m) {
    return m == 800000000 || m == 740000000 || m == 701000000 || m == 702000000 || m == 500000000 || m == 501000000;
}
function currentCountryTown(m) {
    if (m == 800000000) return JP;
    if (m == 740000000) return TW;
    if (m == 701000000 || m == 702000000) return CN;
    if (m == 500000000 || m == 501000000) return TH;
    return -1;
}

function start() {
    status = -1;
    action(1, 0, 0);
}

function action(mode, type, selection) {
    if (mode == -1) { cm.dispose(); return; }
    if (mode == 0 && status == 0) { cm.dispose(); return; }
    if (mode == 1) status++; else status--;

    if (isTourMap(cm.getMapId())) {
        tour(selection);
    } else {
        home(selection);
    }
}

// ===== 在家鄉城鎮:出發去國際村 =====
function home(selection) {
    var job = cm.getJob();
    cost = (job == 0 || job == 1000 || job == 2000 || job == 2001) ? 300 : 3000;

    if (status == 0) {
        cm.sendNext("如果對疲倦的生活感到厭煩，何不去旅行呢？不僅能感受異國文化，還能增廣見聞！\r\n我們#b楓之谷旅行社#k推出的#b世界旅行#k，只要#b" + cost + " 楓幣#k，就帶您暢遊各國！");
    } else if (status == 1) {
        dests = ["#b日本#k － 古代神社", "#b台灣#k － 西門町", "#b中國#k － 上海灘", "#b泰國#k － 水上市場"];
        destMaps = [JP, TW, CN, TH];
        var msg = "目前我們提供以下的世界旅行地點，請問您想去哪裡呢？（費用 #b" + cost + " 楓幣#k）";
        for (var i = 0; i < dests.length; i++) msg += "\r\n#L" + i + "#" + dests[i] + "#l";
        cm.sendSimple(msg);
    } else if (status == 2) {
        sel = selection;
        cm.sendYesNo("您想前往 " + dests[sel] + " 嗎？\r\n將收取 #b" + cost + " 楓幣#k，準備好就出發囉！");
    } else if (status == 3) {
        if (cm.getMeso() < cost) {
            cm.sendOk("您身上的楓幣不足 #b" + cost + "#k，無法出發喔。");
            cm.dispose(); return;
        }
        cm.gainMeso(-cost);
        cm.saveLocation("WORLDTOUR");
        cm.warp(destMaps[sel], 0);
        cm.dispose();
    }
}

// ===== 在國際村:跳轉其他國際村 / 回家 =====
function tour(selection) {
    if (status == 0) {
        var back = cm.getSavedLocation("WORLDTOUR");
        var cur = currentCountryTown(cm.getMapId());
        var all = [[JP, "#b日本#k － 古代神社"], [TW, "#b台灣#k － 西門町"], [CN, "#b中國#k － 上海灘"], [TH, "#b泰國#k － 水上市場"]];
        dests = []; destMaps = [];
        for (var i = 0; i < all.length; i++) {
            if (all[i][0] == cur) continue;
            destMaps.push(all[i][0]); dests.push(all[i][1]);
        }
        var msg = "旅途愉快嗎？想繼續前往其他國家，還是回家呢？";
        for (var j = 0; j < dests.length; j++) msg += "\r\n#L" + j + "#" + dests[j] + "（#b" + HOP + " 楓幣#k）#l";
        msg += "\r\n#L9#我玩夠了，帶我回 #m" + (back == -1 ? 100000000 : back) + "##l";
        cm.sendSimple(msg);
    } else if (status == 1) {
        if (selection == 9) {
            var back = cm.getSavedLocation("WORLDTOUR");
            cm.warp(back == -1 ? 100000000 : back, 0);
            cm.clearSavedLocation("WORLDTOUR");
            cm.dispose(); return;
        }
        sel = selection;
        cm.sendYesNo("要前往 " + dests[sel] + " 嗎？將收取 #b" + HOP + " 楓幣#k。");
    } else if (status == 2) {
        if (cm.getMeso() < HOP) {
            cm.sendOk("您身上的楓幣不足 #b" + HOP + "#k。");
            cm.dispose(); return;
        }
        cm.gainMeso(-HOP);
        cm.warp(destMaps[sel], 0);
        cm.dispose();
    }
}
