using System;
using System.Collections.Generic;
using UnityEngine.Localization.Settings;

namespace BetterSkinRolls
{
    /// <summary>The mod's own texts (its Options tab) in every language the game ships.</summary>
    public static class Strings
    {
        public const string SelfToggle = "SelfToggle";
        public const string SelfHint = "SelfHint";
        public const string StrikeToggle = "StrikeToggle";
        public const string StrikeHint = "StrikeHint";

        public static event Action LanguageChanged;

        private static bool _subscribed;

        // Locale codes as the game's Localization settings report them.
        private static readonly Dictionary<string, Dictionary<string, string>> Table = new Dictionary<string, Dictionary<string, string>>
        {
            [SelfToggle] = new Dictionary<string, string>
            {
                ["en"] = "Skip completed weapons",
                ["zh-CN"] = "跳过已集齐的武器",
                ["zh-TW"] = "跳過已集齊的武器",
                ["fr"] = "Ignorer les armes complètes",
                ["de"] = "Vollständige Waffen überspringen",
                ["it"] = "Salta armi complete",
                ["ja"] = "コンプリートした武器をスキップ",
                ["ko"] = "완성한 무기 건너뛰기",
                ["pl"] = "Pomijaj ukończone bronie",
                ["pt-BR"] = "Pular armas completas",
                ["ru"] = "Пропускать собранное оружие",
                ["es-MX"] = "Omitir armas completas",
                ["es-ES"] = "Omitir armas completas",
                ["sv"] = "Hoppa över kompletta vapen",
                ["tr"] = "Tamamlanan silahları atla",
                ["uk"] = "Пропускати зібрану зброю",
            },
            [SelfHint] = new Dictionary<string, string>
            {
                ["en"] = "Your rolls leave out weapons whose skins you've all unlocked. If the host doesn't have the mod, only you see the changed wheel.",
                ["zh-CN"] = "你转动时会排除皮肤已全部解锁的武器。如果房主没有此模组，只有你能看到修改后的转盘。",
                ["zh-TW"] = "你轉動時會排除外觀已全部解鎖的武器。如果房主沒有此模組，只有你看得到修改後的轉輪。",
                ["fr"] = "Vos tirages excluent les armes dont toutes les apparences sont débloquées. Si l'hôte n'a pas le mod, vous seul voyez la roue modifiée.",
                ["de"] = "Deine Drehungen lassen Waffen aus, deren Skins du alle freigeschaltet hast. Hat der Host den Mod nicht, siehst nur du die geänderte Walze.",
                ["it"] = "I tuoi giri escludono le armi con tutte le skin sbloccate. Se l'host non ha la mod, solo tu vedi la ruota modificata.",
                ["ja"] = "スキンをすべて解放した武器は、あなたが回すときに除外されます。ホストがMODを入れていない場合、変更された結果はあなたにだけ表示されます。",
                ["ko"] = "스킨을 모두 해금한 무기는 내가 돌릴 때 제외됩니다. 호스트에 모드가 없으면 바뀐 결과는 나에게만 보입니다.",
                ["pl"] = "Twoje losowania pomijają bronie, których wszystkie skórki odblokowałeś. Jeśli host nie ma moda, zmienione losowanie widzisz tylko ty.",
                ["pt-BR"] = "Seus giros deixam de fora armas com todos os visuais desbloqueados. Se o host não tiver o mod, só você vê a roleta alterada.",
                ["ru"] = "Ваши прокруты пропускают оружие, у которого открыты все скины. Если у хоста нет мода, изменённый барабан видите только вы.",
                ["es-MX"] = "Tus giros dejan fuera las armas con todos sus aspectos desbloqueados. Si el anfitrión no tiene el mod, solo tú ves la ruleta cambiada.",
                ["es-ES"] = "Tus tiradas dejan fuera las armas con todos sus diseños desbloqueados. Si el anfitrión no tiene el mod, solo tú ves la ruleta cambiada.",
                ["sv"] = "Dina snurr hoppar över vapen där du har låst upp alla skins. Om värden saknar modden ser bara du den ändrade snurren.",
                ["tr"] = "Çevirişlerin, tüm kaplamalarının kilidini açtığın silahları dışarıda bırakır. Kurucuda mod yoksa değişen çarkı yalnızca sen görürsün.",
                ["uk"] = "Ваші прокрути пропускають зброю, у якої відкриті всі скіни. Якщо в хоста немає мода, змінений барабан бачите лише ви.",
            },
            [StrikeToggle] = new Dictionary<string, string>
            {
                ["en"] = "Cross out completed weapons in the prize list",
                ["zh-CN"] = "在奖品列表中划掉已集齐的武器",
                ["zh-TW"] = "在獎品列表中劃掉已集齊的武器",
                ["fr"] = "Barrer les armes complètes dans la liste des prix",
                ["de"] = "Vollständige Waffen in der Preisliste durchstreichen",
                ["it"] = "Barra le armi complete nell'elenco dei premi",
                ["ja"] = "景品リストでコンプリート済みの武器に取り消し線",
                ["ko"] = "상품 목록에서 완성한 무기에 취소선",
                ["pl"] = "Przekreślaj ukończone bronie na liście nagród",
                ["pt-BR"] = "Riscar armas completas na lista de prêmios",
                ["ru"] = "Зачёркивать собранное оружие в списке призов",
                ["es-MX"] = "Tachar armas completas en la lista de premios",
                ["es-ES"] = "Tachar armas completas en la lista de premios",
                ["sv"] = "Stryk över kompletta vapen i prislistan",
                ["tr"] = "Ödül listesinde tamamlanan silahların üstünü çiz",
                ["uk"] = "Закреслювати зібрану зброю у списку призів",
            },
            [StrikeHint] = new Dictionary<string, string>
            {
                ["en"] = "The prize list on the slot machine crosses out the weapons you've completed. Only you see the marks.",
                ["zh-CN"] = "老虎机上的奖品列表会划掉你已集齐的武器。只有你能看到这些标记。",
                ["zh-TW"] = "拉霸機上的獎品列表會劃掉你已集齊的武器。只有你看得到這些標記。",
                ["fr"] = "La liste des prix de la machine à sous barre les armes que vous avez complétées. Vous seul voyez ces marques.",
                ["de"] = "Die Preisliste am Spielautomaten streicht deine vollständigen Waffen durch. Nur du siehst die Markierung.",
                ["it"] = "L'elenco dei premi sulla slot machine barra le armi che hai completato. Solo tu vedi i segni.",
                ["ja"] = "スロットマシンの景品リストで、コンプリート済みの武器に取り消し線が付きます。この印はあなたにだけ見えます。",
                ["ko"] = "슬롯머신의 상품 목록에서 완성한 무기에 취소선이 그어집니다. 이 표시는 나에게만 보입니다.",
                ["pl"] = "Lista nagród na automacie przekreśla bronie, które ukończyłeś. Tylko ty widzisz te oznaczenia.",
                ["pt-BR"] = "A lista de prêmios da máquina risca as armas que você já completou. Só você vê essas marcas.",
                ["ru"] = "Список призов на автомате зачёркивает собранное оружие. Эти отметки видите только вы.",
                ["es-MX"] = "La lista de premios de la tragamonedas tacha las armas que completaste. Solo tú ves las marcas.",
                ["es-ES"] = "La lista de premios de la tragaperras tacha las armas que has completado. Solo tú ves las marcas.",
                ["sv"] = "Prislistan på spelautomaten stryker över vapnen du har gjort klara. Bara du ser markeringarna.",
                ["tr"] = "Slot makinesindeki ödül listesi tamamladığın silahların üstünü çizer. Bu işaretleri yalnızca sen görürsün.",
                ["uk"] = "Список призів на автоматі закреслює зібрану зброю. Ці позначки бачите лише ви.",
            },
        };

        public static string Get(string key, params object[] args)
        {
            EnsureSubscribed();
            if (!Table.TryGetValue(key, out var texts))
                return key;

            string text = null;
            string code = CurrentCode();
            if (code != null && !texts.TryGetValue(code, out text))
            {
                // e.g. "es" or "es-AR" → first entry for that language
                string language = code.Split('-')[0];
                foreach (var pair in texts)
                {
                    if (pair.Key.Split('-')[0].Equals(language, StringComparison.OrdinalIgnoreCase))
                    {
                        text = pair.Value;
                        break;
                    }
                }
            }
            if (text == null)
                text = texts["en"];
            return args.Length > 0 ? string.Format(text, args) : text;
        }

        private static string CurrentCode()
        {
            try
            {
                var code = LocalizationSettings.SelectedLocale?.Identifier.Code;
                if (string.IsNullOrEmpty(code))
                    return null;
                // Match the table's casing ("zh-cn" → "zh-CN").
                var parts = code.Split('-');
                return parts.Length == 2 ? parts[0].ToLowerInvariant() + "-" + parts[1].ToUpperInvariant() : code.ToLowerInvariant();
            }
            catch (Exception)
            {
                return null; // Localization not initialized yet
            }
        }

        private static void EnsureSubscribed()
        {
            if (_subscribed)
                return;
            try
            {
                LocalizationSettings.SelectedLocaleChanged += _ => LanguageChanged?.Invoke();
                _subscribed = true;
            }
            catch (Exception)
            {
                // Try again on the next lookup.
            }
        }
    }
}
