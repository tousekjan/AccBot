_Read the README in [English](https://github.com/Crynners/AccBot/blob/main/README.en.md)._

# Úvod
Vítejte na stránkách AccBota. AccBot je open-source akumulační bot, který v pravidelných intervalech po malých částkách v Kč nebo Eurech nakupuje [BTC](https://cs.wikipedia.org/wiki/Bitcoin) _(eventuálně LTC, ETH, XMR nebo DASH)_ na burze [Coinmate](https://coinmate.io/) dle strategie [DCA](https://www.fxstreet.cz/jiri-makovsky-co-je-dollar-cost-averaging-a-jak-funguje.html).

# Jednoduchý popis fungování bota
* Nakupuje uživatelem definovanou částku v českých korunách _(typicky desítky Kč)_ / eurech _(typicky jednotky Eur)_ každých uživatelsky definovaných hodin _(ideálně dělitelných 24, aby nakupoval vždy ve stejný čas, tedy např. -> každou hodinu, 1x za 2h, 1x za 4h, 1x za 8h, etc.)_.
* Běží autonomně bez nutnosti jej nějak v čase spravovat, je zapotřebí si pouze hlídat stav svého Kč účtu a pravidelně jej na Coinmate doplňovat _(např. jednou za měsíc)_.
* **Náklady na provoz jsou prakticky nulové** (vychází to cca na 0.04 € / měsíčně za Azure hosting); bot je implementován zatím jako [Azure function](https://azure.microsoft.com/cs-cz/services/functions/), která se spouští v pravidelných intervalech a celé řešení je tedy hostované na [Azure](https://azure.microsoft.com/cs-cz/). 
* (Volitelná funkcionalita) Po každém nákupu Vás informuje na Telegramovém kanále o tom, za jakou částku nakoupil. Tuto informaci doplní o statistiky, jaká je aktuální průměrná akumulovaná cena, etc. Viz příklad:
  * ![image](https://user-images.githubusercontent.com/87997650/127355720-fe73c0b5-5fd4-4d31-98dc-b569975f8a9e.png)
* (Volitelná funkcionalita) Pokud je naakumulované dostatečné množství BTC, pak pokud je poplatek za výběr z celkové částky menší, než uživatelsky stanovený limit (např. 0.1 %), bot pošle naakumulované množství BTC z burzy do definované BTC peněženky (poznámka: pokud chcete využívat tuto funkcionalitu, doporučujeme povolit API odeslání pouze na Vaši konkrétní BTC peněženku, viz nastavení při vytváření API klíče na Coinmate)
  * ![image](https://user-images.githubusercontent.com/87997650/127356371-6a9d1493-55f0-41cc-ab03-4a67cf610f42.png)

# Prerekvizity
1. **Nainstalovaný [PowerShell](https://docs.microsoft.com/cs-cz/powershell/scripting/install/installing-powershell?view=powershell-7.1)**
2. **Nainstalovaný [Azure CLI](https://docs.microsoft.com/cs-cz/cli/azure/install-azure-cli)**
3. **Založený účet na burze [Coinmate](https://coinmate.io/)** (účet je zdarma; k tomu, abyste mohli na burzu zasílat fiat, je zapotřebí provést ověření [KYC](https://en.wikipedia.org/wiki/Know_your_customer))
    - Pokud byste nás chtěli podpořit a zaregistrovat se přes náš referral link, můžete kliknutím na banner níže

    <a href="https://coinmate.io?referral=ZWw4NVlXbDRVbTFVT0dKS1ZHczBZMXB1VEhKTlVRPT0"><img src="https://coinmate.io/static/img/banner/CoinMate_Banner_02.png" alt="Registrační odkaz přes referral" border="0"></a>


4. **Založený účet na [Azure](https://azure.microsoft.com/cs-cz/)** (účet je zdarma; platí se pouze za využité prostředky, které vychází na cca 0.04$ / měsíc)

# Postup instalace
1. Na Coinmate si [vygenerujte API klíče](https://coinmate.io/blog/using-the-coinmate-io-api/) (aby měl BOT přístup k prostředkům na burze a mohl provádět svoji akumulační činnost). Do poznámkového bloku si zapište vygenerovaný ClientId, PublicKey a PrivateKey -> budete je potřebovat v bodu 5.
   - POZOR: Je nutné API klíčům přidat oprávnění na Trading, viz: 

   ![image](https://user-images.githubusercontent.com/87997650/127633515-b5828914-6183-4c60-8208-4e78d262f62e.png). 
   - Pokud byste chtěli využít i funkci automatického výběru, zaškrtněte i volbu "Enable for Withdrawal". V takovém případě doporučujeme si zaškrtnout i "Enable for withdrawals to template addresses only", což znanená, že bot bude moci poslat naakumulované BTC pouze na Vámi definované adresy, viz: 

   ![image](https://user-images.githubusercontent.com/87997650/127633656-a6698455-03b6-4b23-902d-e5642dbe4988.png)

3. Stáhněte si [ZIP z aktuálního RELEASE](https://github.com/Crynners/AccBot/releases/latest/download/AccBot_installation.zip), který obsahuje instalační PowerShell skript a zbuilděného bota.
4. ZIP z předchozího bodu rozbalte kamkoliv do Vašeho souborového systému
5. V poznámkovém bloku (nebo jiném textovém editoru) otevřte soubor **install_script.ps1**
6. (Nepovinné) Nastavte si [Telegram notifikace](#telegramnotifications). _(Pokud i přes doporučení nechcete Telegram notifikace využívat, v dalším kroku proměnné týkající se Telegramu nevyplňujte)_
7. Upravte proměnné v sekci **### USER-DEFINED VARIABLES ###**
```powershell
##############################
### USER-DEFINED VARIABLES ###
##############################

# Jméno, které se zobrazuje v Telegram notifikacích
$Name='anonymous'

# Crypto, které na Coinmate chcete nakupovat (MOŽNÉ HODNOTY: BTC, LTC, ETH, XRP, DASH)
$Currency='BTC'

# Fiat měna, za kterou chcete na Coinmate nakupovat crypto (MOŽNÉ HODNOTY: CZK, EUR)
$Fiat='CZK'

# Velikost chunku v CZK, resp. EUR, který chcete pravidelně nakupovat (MINIMUM pro CZK: 26; MINIMUM pro EUR: 1)
$ChunkSize='26'

# Jednou za kolik hodin chcete pravidelně nakupovat BTC
$HourDivider='1'

# Příznak, zdali chcete povolit Withdrawal v případě, že je fee menší než 0.1% (POVOLENÉ HODNOTY: true / false)
$WithdrawalEnabled='false'

# Adresa peněženky pro withdraw (aplikuje se pouze pokud WithdrawalEnabled = TRUE)
$WithdrawalAddress=''

# (Využije se pouze v případě, kdy $WithdrawalEnabled='true'). 
# Maximální limit na withdrawal fee v procentech. (DEFAULT: 0.001 = 0.1 %) 
$MaxWithdrawalPercentageFee = '0.001'

# (Využije se pouze v případě, kdy $WithdrawalEnabled='true'). 
# Maximální limit na withdrawal fee v absolutní hodnotě (Kč)
# Pokud je nastaveno -1, uplatní se pouze podmínka procentuální => $MaxWithdrawalPercentageFee
$MaxWithdrawalAbsoluteFee = -1

# Adresa telegram kanálu, do kterého chcete dostávat notifikace (ve formátu @NázevKanálu)
$TelegramChannel='@channel_name'

# Privátní klíč telegram bota (POZOR, bot musí být členem kanálu výše)
$TelegramBot='telegram_bot_hash'

# ClientId z Coinmate API
$CoinMateCredentials_ClientId='111'

# Public key z Coinmate API
$CoinMateCredentials_PublicKey='XXX'

# Private key z Coinmate API
$CoinMateCredentials_PrivateKey='XXX'

# Příznak pro vytvoření logu na Azure. (POVOLENÉ HODNOTY: true / false). DOPORUČENÍ: Standardně mít vypnuté, tedy "false". 
# Log zvyšuje měsíční náklady z cca 0.04 € / měsíc na cca 0.2 € / měsíc. Doporučujeme tedy zapnout pouze pokud Vám bot například nenakupuje jak by měl. 
$CreateAzureLog = 'false'

##############################
```
<a name="installscript"></a>
7. Uložte soubor **install_script.ps1** s vyplněnými hodnotami z předchozího kroku.
  - <img src="https://user-images.githubusercontent.com/87997650/128522417-9bd02e68-a4d6-48bd-8661-81ec43ee3a47.png" width="25" height="25" />: Poklepáním spusťte **run.bat** file _(Pro Windows OS)._ 
  - <img src="https://user-images.githubusercontent.com/87997650/128523326-a7456256-4f01-41ef-9c21-1fe5968923cf.png" width="25" height="25" /> / <img src="https://user-images.githubusercontent.com/87997650/128523557-566d738d-67f5-43ac-a65e-080105f92abb.png" width="25" height="25" />: Spusťte PowerShell a v něm proveďte příkaz 
    ```powershell 
    powershell.exe -executionpolicy bypass -file .\install_script.ps1
    ``` 
Skript Vám automaticky připraví všechny potřebné resources na Azure. Na začátku by mělo vyskočit i okno s přihlášením do Azure portal. 

**POZOR: Instalace trvá několik minut, vyčkejte prosím na její dokončení.** Na závěr by se měla objevit následující hláška:

![image](https://user-images.githubusercontent.com/87997650/128522145-3acfef81-ede6-4e40-95f0-627a532ca5d2.png)

<a name="telegramnotifications"></a>
# (Nepovinné) Nastavení Telegram notifikací

Tato část není povinná pro provoz bota, nicméně jde o velkou přidanou hodnotu, neboť Vás bot bude pravidelně informovat po každém nákupu jaké je Vaše průměrná cena naakumulovaného BTC a kolik BTC jste již naakumulovali. Zároveň si budete moci v reálném čase ověřovat, že bot funguje.

1. Založení účtu na [Telegramu](https://telegram.org/)
2. Vytvoření bota přes BotFather dle [návodu](https://sendpulse.com/knowledge-base/chatbot/create-telegram-chatbot).
   - Token z návodu se poté vloží do proměnné **$TelegramBot** z PowerShell skriptu
3. Vytvoření nového kanálu ([videonávod](https://youtu.be/q6-k_LGbw_k) pro vytvoření z mobilní aplikace -> [Android](https://play.google.com/store/apps/details?id=org.telegram.messenger&hl=cs&gl=US) nebo [iOS](https://apps.apple.com/us/app/telegram-messenger/id686449807) verze). 
Eventuálně postupujte dle následujících printscreenů -> vytvoření přes [Telegram desktop](https://desktop.telegram.org/).
   - V levém horním rohu klikněte na nastavení
   
   ![image](https://user-images.githubusercontent.com/87997650/127706308-0ca1aead-f5a8-42eb-b740-6463d820636f.png)
   - Klikněte na tlačítko `New Channel`
   
   ![image](https://user-images.githubusercontent.com/87997650/127706363-c10948dd-2d97-4dc1-9028-718d1f802153.png)
   - Pojmenujte si svůj kanál a potvrďte založení tlačítkem
   
   ![image](https://user-images.githubusercontent.com/87997650/127706441-52c861f9-3f76-49a0-8d42-9c5d48c657cc.png)
   - Kanál označte jako **Public** a vymyslete pro něj unikátní název. Tento název se poté vyplňte ve formátu `@MyAccBotChannel` (v případě příkladu níže) do proměnné **$TelegramChannel** v powershell skriptu
   ![image](https://user-images.githubusercontent.com/87997650/127706976-591cb415-4bc2-444b-95fc-56aaa9d58e73.png)
   - Pokud chcete vytvořený kanál nastavit jako **Private** postupujte takto:
   - Zjistěte Id vašeho privátního kanálu, např. tak, že kanál otevřete ve [webovém rozhraní Telegram](https://web.telegram.org)
   - URL adresa bude mít formát https://web.telegram.org/z/#{IdKanálu}
   - Pozor! pro odesílání zpráv přes vytvořeného bota musíte před Id přidat ještě -100. Pokud tedy vaše adresa byla např. https://web.telegram.org/z/#-123456789, výsledné Id bude -100123456789.
   - Toto získané Id vložte do proměnné **$TelegramChannel** namísto názvu kanálu.
4. Do kanálu pozvěte svého bota (vyhledejte ho dle jména), kterého jste vytvořili v bodu 2 přes BotFather.
   ![image](https://user-images.githubusercontent.com/87997650/127707214-174f6dd0-a990-49d8-8cb0-6c9c9e290102.png)
   - Potvrďte bota jako administrátora kanálu
   
   ![image](https://user-images.githubusercontent.com/87997650/127707275-af26e4f8-3c8b-46ff-b437-1e0d29a9ce77.png)
   - Ponechte defaultní volbu oprávnění bota
   
   ![image](https://user-images.githubusercontent.com/87997650/127707327-faa3fa84-56ab-4fce-be0f-7a3f81cadf38.png)
5. Hotovo, do vytvořeného kanálu by Vám odteď měl bot zapisovat informace o nákupech se statistikami.

# Úprava nastavení / Aktualizace již běžícího AccBota

- Pokud Vám AccBot již úspěšně běží a chcete si časem změnit nějaké nastavení _(četnost nebo výše jednotlivých nákupů, povolení withdrawal, etc.)_, nejjednodušším způsobem je upravit **USER-DEFINED VARIABLES** v instalačním skriptu **install_script.ps1** a skript znovu spustit dle kroku 7 [instalačního návodu](#installscript).
- Pokud chcete nasadit novou verzi AccBota, stáhněte si [ZIP z aktuálního RELEASE](https://github.com/Crynners/AccBot/releases/latest/download/AccBot_installation.zip), nastavte si sekci **USER-DEFINED VARIABLES** v instalačním skriptu **install_script.ps1** a skript znovu spustit dle kroku 7 [instalačního návodu](#installscript).

# Donate
![heart_donate](https://user-images.githubusercontent.com/87997650/127650190-188e401a-9942-4511-847e-d1010628777a.png)

AccBota jsme se rozhodli poskytnout zcela zdarma, neboť chceme co nejvíce lidem umožnit co nejjednodušší a nejlevnější cestu k aplikaci strategie [DCA](https://www.fxstreet.cz/jiri-makovsky-co-je-dollar-cost-averaging-a-jak-funguje.html). Věříme, že pravidelné spoření v Bitcoinu je tím nejlepším způsobem k zajištění do budoucna. Investovat do BTC se zapojením emocí a s ambicemi predikovat trh se totiž ve většině případů nevyplácí.

Pokud byste nás chtěli podpořit, rozhodně se tomu bránit nebudeme. Níže jsou uvedeny jednotlivé peněženky, kam nám můžete zaslat příspěvek třeba na pivo. :) Děkujeme <3

- **BTC ❤️**: bc1q2hz79m4csklecqgusu9e2yjnrr6e9ca6nhu0at
     - <img src="https://user-images.githubusercontent.com/87997650/127651099-d9e1b381-adcf-46a5-9d17-59f87176304d.png" width="150" height="150" />
- **LTC**: LTXdCFBYHgVLa8cBNBqwEvaQLi8tENY5R3
     - <img src="https://user-images.githubusercontent.com/87997650/127651223-0abe025d-950b-445e-8196-7c113853b313.png" width="150" height="150" />
- **XMR**: 49QBko3UdegAkx6g8foqjs9efQD6rrhsPEoTqP9HmA2LCUZsJ8xBD2JZSMEdzhA5NJ9SrVhzu2uJXRUvL2kAiV45LyDBCUt
     - <img src="https://user-images.githubusercontent.com/87997650/127651801-cc35dfc0-f1ce-4dd0-ae0f-211fab41e2fb.png" width="150" height="150" />
- **DOGE**: DR9mEaVLmx3gxqiqffwYQcLsT1upRL3xe9
     - <img src="https://user-images.githubusercontent.com/87997650/127651630-2bb06de7-3b7a-42af-86b0-8b8fa0a6d59a.png" width="150" height="150" />
- **ETH**: 0x8A944bcb5919dF04C5207939749C40A61f88188C
     - <img src="https://user-images.githubusercontent.com/87997650/127653313-e989e607-f1db-40e9-a341-7cbdbd9fdfd0.png" width="150" height="150" />
- **DOT**: 15sBCVyWu5Gy9VnzQpid4ggC1MmguBBd1xotUVbsbbRWddun
     - <img src="https://user-images.githubusercontent.com/87997650/127651761-6484c9f4-547c-475e-a5ec-2029e6ee1699.png" width="150" height="150" />
- **BNB**: bnb1lwcgq8emrjgptxg4hm37d5tf2yunrph842awrh
     - <img src="https://user-images.githubusercontent.com/87997650/127651542-1fa0b32b-ed30-4a9a-b1cd-1622ef1044cf.png" width="150" height="150" />
- **ADA**: addr1qxgfp7xf8rpg7laque78queavpfdztajgl3hr8kuanuqgdysjruvjwxz3al6penuwpen6czj6yhmy3lrwx0dem8cqs6qr8y8fj
     - <img src="https://user-images.githubusercontent.com/87997650/127651500-df50eaee-15aa-415e-8a0b-044f22d89493.png" width="150" height="150" />


