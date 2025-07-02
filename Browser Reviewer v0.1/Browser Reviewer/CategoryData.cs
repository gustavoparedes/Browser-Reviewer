

namespace Browser_Reviewer
{
    public static class CategoryData
    {

        public static readonly Dictionary<string, HashSet<string>> categoryDomains = new Dictionary<string, HashSet<string>>()
        {

            //Agregarlos sin la www, p.e yahoo.com, y NO www.yahoo.com, la funcion que evalua la url recortara el https://www.yahoo.com reduciendola a yahoo.com para ser comparada con los
            //nombres de dominio abajo listados por categorias.

            { "Google", new HashSet<string>
                {
                    "google.com", "mail.google.com"

                }
            },

            { "YouTube", new HashSet<string>
                {
                    "youtube.com"

                }
            },


            { "Facebook", new HashSet<string>
                {
                    "facebook.com"
                    
                }
            },

            { "Social Media", new HashSet<string>
                {
                    "x.com", "twitter.com", "instagram.com", "linkedin.com", "snapchat.com",
                    "pinterest.com", "tiktok.com", "reddit.com", "tumblr.com", "whatsapp.com",
                    // Añadir hasta 50 dominios populares de redes sociales
                }
            },

            { "Shopping", new HashSet<string>
                {
                    "amazon.com", "amazon.co.uk", "amazon.de", "amazon.fr", "amazon.it",
                    "amazon.es", "amazon.ca", "amazon.com.au", "amazon.com.br", "amazon.com.mx",
                    "amazon.co.jp", "ebay.com", "ebay.co.uk", "ebay.de", "ebay.fr",
                    "ebay.it", "ebay.es", "ebay.ca", "aliexpress.com", "walmart.com",
                    "walmart.ca", "newegg.com", "bestbuy.com", "flipkart.com", "target.com",
                    "costco.com", "ikea.com", "ikea.co.uk", "craigslist.org", "alibaba.com",
                    "rakuten.com", "asos.com", "etsy.com", "wayfair.com", "carrefour.com",
                    "shopify.com", "argos.co.uk", "very.co.uk", "tesco.com", "myntra.com",
                    "snapdeal.com", "overstock.com", "lazada.com", "shopee.com", "elcorteingles.es",
                    "fnac.com", "mediamarkt.com", "saturn.de", "bol.com", "zalando.com",
                    "jdsports.co.uk", "yoox.com", "macys.com", "nordstrom.com", "bloomingdales.com",
                    "harrods.com", "selfridges.com", "matchesfashion.com", "farfetch.com", "primark.com",
                    "mango.com", "hm.com", "zara.com", "pullandbear.com", "bershka.com",
                    "massimodutti.com", "uniqlo.com", "c-and-a.com", "next.co.uk", "tkmaxx.com",
                    "decathlon.com", "sportsdirect.com", "adidas.com", "nike.com", "puma.com",
                    "underarmour.com", "reebok.com", "asics.com", "timberland.com", "newbalance.com",
                    "vans.com", "converse.com", "sephora.com", "ulta.com", "bathandbodyworks.com",
                    "lush.com", "thebodyshop.com", "bedbathandbeyond.com", "walmart.com.mx", "target.com.au",
                    "homebase.co.uk", "johnlewis.com", "debenhams.com", "houseoffraser.co.uk", "harrods.co.uk",
                    "fortnumandmason.com", "waitrose.com", "ocado.com", "woolworths.com.au", "coles.com.au",
                    "morrisons.com", "asda.com", "iceland.co.uk", "wilko.com", "bmstores.co.uk",
                    "poundland.co.uk", "therange.co.uk", "aldi.com", "aldi.co.uk", "aldi.de",
                    "lidl.com", "lidl.co.uk", "lidl.de", "homebargains.co.uk", "marksandspencer.com",
                    "bigw.com.au", "kmart.com.au", "homedepot.com", "lowes.com", "wayfair.co.uk",
                    "overstock.com", "walmart.ca", "target.com.mx", "bestbuy.ca", "costco.ca",
                    "ikea.ca", "ikea.com.mx", "amazon.in", "ebay.in", "flipkart.com",
                    "myntra.com", "snapdeal.com", "aliexpress.com", "jd.com", "tmall.com",
                    "taobao.com", "mercadolibre.com", "mercadolivre.com.br", "mercadolibre.com.mx", "mercadolibre.com.ar",
                    "mercadolibre.cl", "mercadolibre.com.co", "mercadolibre.com.ve", "ikea.com.br", "aliexpress.ru",
                    "yandex.market", "wildberries.ru", "ozon.ru", "lamoda.ru", "citilink.ru",
                    "e-katalog.ru", "mvideo.ru", "dns-shop.ru", "ozon.ru", "allegro.pl",
                    "ceneo.pl", "alibaba.com", "alibaba.cn", "1688.com", "tokopedia.com",
                    "bukalapak.com", "shopee.co.id", "lazada.co.id", "blibli.com", "shopclues.com",
                    "daraz.com.bd", "sendo.vn", "tiki.vn", "thegioididong.com", "dienmayxanh.com",
                    "fptshop.com.vn", "mediamarkt.nl", "coolblue.nl", "bol.com", "alternate.nl",
                    "amazon.nl", "vandenborre.be", "fnac.be", "coolblue.be", "delhaize.be",
                    "carrefour.be", "ebay.fr", "cdiscount.com", "fnac.com", "darty.com",
                    "boulanger.com", "auchan.fr", "rueducommerce.fr", "laredoute.fr", "ikea.fr",
                    "castorama.fr", "leroymerlin.fr", "amazon.fr", "amazon.de", "otto.de",
                    "mediamarkt.de", "saturn.de", "zalando.de", "galeria.de", "real.de",
                    "conrad.de", "notebooksbilliger.de", "ikea.de", "hornbach.de", "bauhaus.info",
                    "amazon.es", "elcorteingles.es", "mediamarkt.es", "fnac.es", "pccomponentes.com",
                    "carrefour.es", "ikea.es", "leroymerlin.es", "amazon.it", "mediaworld.it",
                    "unieuro.it", "eprice.it", "comet.it", "ikea.it", "amazon.co.uk",
                    "argos.co.uk", "very.co.uk", "currys.co.uk", "johnlewis.com", "next.co.uk",
                    "marksandspencer.com", "asos.com", "boohoo.com", "zara.com", "ikea.co.uk",
                    "screwfix.com", "b&q.com", "wickes.co.uk", "debenhams.com", "sportsdirect.com",
                    "adidas.co.uk", "nike.co.uk", "puma.co.uk", "underarmour.co.uk", "reebok.co.uk",
                    "converse.co.uk", "vans.co.uk", "newbalance.co.uk", "macys.com", "kohls.com",
                    "jcp.com", "walmart.com", "target.com", "bestbuy.com", "homedepot.com",
                    "lowes.com", "sears.com", "newegg.com", "bhphotovideo.com", "wayfair.com",
                    "overstock.com", "ikea.com", "bedbathandbeyond.com", "containerstore.com", "walmart.ca",
                    "canadiantire.ca", "bestbuy.ca", "costco.ca", "ikea.ca", "homedepot.ca",
                    "wayfair.ca", "argos.co.uk", "lidl.co.uk", "aldi.co.uk", "ebay.de",
                    "mediamarkt.de", "ikea.de", "otto.de", "amazon.de", "cdiscount.com",
                    "fnac.com", "rueducommerce.fr", "boulanger.com", "leroymerlin.fr", "castorama.fr",
                    "auchan.fr", "ikea.fr", "mercadolibre.com.mx", "mercadolivre.com.br", "mercadolibre.com.ar",
                    "mercadolibre.cl", "mercadolibre.com.co", "mercadolibre.com.ve", "lazada.com.my", "shopee.com.my",
                    "zalora.com.my", "ikea.com.my", "mercadolibre.com.uy", "mercadolibre.com.bo", "mercadolibre.com.pe",
                    "mercadolibre.com.py", "mercadolibre.com.ec", "mercadolibre.com.gt", "mercadolibre.com.hn", "mercadolibre.com.ni",
                    "mercadolibre.com.pa", "mercadolibre.com.do", "mercadolibre.com.cu", "mercadolibre.com.pr", "mercadolibre.com.sv",
                }
            },

            { "News", new HashSet<string>
                    {
                    // América del Norte
                    "nytimes.com", "cnn.com", "washingtonpost.com", "foxnews.com", "nbcnews.com",
                    "abcnews.go.com", "cbsnews.com", "usatoday.com", "latimes.com", "wsj.com",
                    "theglobeandmail.com", "cbc.ca", "ctvnews.ca", "globalnews.ca", "nationalpost.com","thescore.com",

                    // América Latina
                    "clarin.com", "lanacion.com.ar", "infobae.com", "pagina12.com.ar", "folha.uol.com.br",
                    "globo.com", "terra.com.br", "elcomercio.pe", "larepublica.pe", "eluniverso.com",
                    "eltiempo.com", "elespectador.com", "elheraldo.co", "excelsior.com.mx", "milenio.com",
                    "reforma.com", "eluniversal.com.mx", "jornada.com.mx", "la-razon.com", "la-razon.com.bo",
                    "elespectador.com","elespectador.com.co",

                    // Europa
                    "bbc.com", "theguardian.com", "telegraph.co.uk", "independent.co.uk", "reuters.com",
                    "dailymail.co.uk", "ft.com", "lemonde.fr", "lefigaro.fr", "liberation.fr",
                    "elpais.com", "elmundo.es", "abc.es", "expansion.com", "corriere.it",
                    "repubblica.it", "lastampa.it", "bild.de", "spiegel.de", "faz.net",
                    "welt.de", "dw.com", "derstandard.at", "kronenzeitung.at", "rtbf.be",
                    "rte.ie", "irishtimes.com", "morgenpost.de", "france24.com", "euronews.com",

                    // Asia
                    "scmp.com", "straitstimes.com", "channelnewsasia.com", "nhk.or.jp", "asahi.com",
                    "mainichi.jp", "japantimes.co.jp", "yomiuri.co.jp", "xinhuanet.com", "chinadaily.com.cn",
                    "people.cn", "koreatimes.co.kr", "koreaherald.com", "hindustantimes.com", "indiatimes.com",
                    "thehindu.com", "dawn.com", "geo.tv", "theaustralian.com.au", "smh.com.au",
                    "news.com.au", "afr.com", "nzherald.co.nz", "stuff.co.nz", "theage.com.au",

                    // África
                    "news24.com", "sabcnews.com", "ewn.co.za", "citizen.co.za", "timeslive.co.za",
                    "iol.co.za", "standardmedia.co.ke", "nation.africa", "monitor.co.ug", "theeastafrican.co.ke",
                    "punchng.com", "vanguardngr.com", "thecable.ng", "guardian.ng", "sowetanlive.co.za", 

                    // Medio Oriente
                    "aljazeera.com", "arabnews.com", "gulfnews.com", "haaretz.com", "timesofisrael.com",
                    "thenationalnews.com", "khaleejtimes.com", "jerusalemonline.com", "egypttoday.com", "al-monitor.com"
                    }
            },

            { "Entertainment", new HashSet<string>
                {
                    "netflix.com", "hulu.com", "disneyplus.com", "spotify.com",
                    "twitch.tv", "hbo.com", "primevideo.com", "vimeo.com", "soundcloud.com", "pandora.com"
                    // Añadir más dominios de sitios de entretenimiento
                }
            },

            
            { "Airlines", new HashSet<string>
               {
                    "qatarairways.com", "singaporeair.com", "emirates.com", "ana.co.jp", "cathaypacific.com",
                    "jal.co.jp", "turkishairlines.com", "evaair.com", "airfrance.com", "swiss.com",
                    "koreanair.com", "hainanairlines.com", "britishairways.com", "fijiairways.com", "iberia.com",
                    "airvistara.com", "virginatlantic.com", "lufthansa.com", "etihad.com", "saudia.com",
                    "delta.com", "airnewzealand.com", "finnair.com", "qantas.com", "omanair.com",
                    "klm.com", "aeroflot.ru", "garuda-indonesia.com", "asiana.com", "vietnamairlines.com",
                    "aeromexico.com", "egyptair.com", "tapairportugal.com", "latam.com", "saa.co.za",
                    "aircanada.com", "alaskaair.com", "austrian.com", "sas.se", "brusselsairlines.com",
                    "airmauritius.com", "airserbia.com", "airbaltic.com", "azerbaijanairlines.com", "airastana.com",
                    "philippineairlines.com", "malaysiaairlines.com", "royalairmaroc.com", "srilankan.com", "elal.com",
                    "aireuropa.com", "avianca.com"
                }
            },

            { "Hotels and Rentals", new HashSet<string>
                {
                    "booking.com", "airbnb.com", "hotels.com", "expedia.com", "agoda.com",
                    "trivago.com", "vrbo.com", "orbitz.com", "kayak.com", "priceline.com",
                    "tripadvisor.com", "travelocity.com", "hostelworld.com", "choicehotels.com", "hilton.com",
                    "marriott.com", "ihg.com", "hyatt.com", "accorhotels.com", "radissonhotels.com"
                }
            },



            { "Search Engine", new HashSet<string>
                {
                    "bing.com", "yahoo.com", "duckduckgo.com", "baidu.com", "search.brave.com",
                    "ask.com", "aol.com", "wolframalpha.com", "yandex.com", "ecosia.org", "qwant.com",
                    "seznam.cz", "swisscows.com", "gigablast.com"
                    // Añadir más dominios de motores de búsqueda si es necesario
                }
            },

            { "Code Hosting", new HashSet<string>
                {
                    "github.com", "gitlab.com", "bitbucket.org", "sourceforge.net", "dev.azure.com",
                    "codecommit.aws.amazon.com", "gitea.io", "phabricator.org", "launchpad.net",
                    "source.developers.google.com"
                    // Añadir más dominios de plataformas de alojamiento de código
                }
            },



            { "Webmail", new HashSet<string>
                {
                    "gmail.com", "accounts.google.com", "mail.yahoo.com", "login.yahoo.com", "ymail.com", "rocketmail.com",
                    "outlook.com", "live.com", "login.live.com", "hotmail.com", "msn.com",
                    "protonmail.com", "zoho.com", "aol.com", "mail.com",
                    "icloud.com", "me.com", "mac.com", "yandex.com", "yandex.ru",
                    "gmx.com", "gmx.net", "tutanota.com", "fastmail.com",
                    "mail.ru", "hey.com", "yahoo.co.jp", "rediffmail.com",
                    "lycos.com", "hushmail.com", "runbox.com", "q.com",
                    "zoho.com", "gmx.co.uk", "gmx.de", "zimbra.com",
                    "163.com", "126.com", "sina.com", "qq.com",
                    "naver.com", "daum.net"
                    // Añadir más si es necesario
                }
            },


            { "File Encryption Tools", new HashSet<string>
                {
                    "axcrypt.net", "veracrypt.fr", "cryptomator.org", "boxcryptor.com",
                    "truecrypt.org", "gpg4win.org", "7-zip.org", "diskcryptor.net",
                    "kruptos2.co.uk", "securstick.com", "aescrypt.com", "gnupg.org",
                    "nordlocker.com"
                    // Añadir más si es necesario
                }
            },

            { "Cloud Storage Services", new HashSet<string>
                {
                    "dropbox.com", "drive.google.com", "google.com/drive", "onedrive.com",
                    "icloud.com", "box.com", "amazon.com/clouddrive", "pcloud.com", "mega.nz",
                    "sync.com", "tresorit.com", "spideroak.com", "backblaze.com", "mediafire.com",
                    "disk.yandex.com", "yandex.com", "degoo.com", "jottacloud.com", "nextcloud.com",
                    "owncloud.com", "owncloud.org", "sugarsync.com", "idrive.com", "hubic.com",
                    "zoho.com/workdrive", "livedrive.com", "icedrive.net", "koofr.eu", "syncplicity.com",
                    "sharefile.com", "alibabacloud.com", "wasabi.com", "egnyte.com", "nordlocker.com",
                    "filecloud.com", "zoolz.com", "terabox.com"
                    // Añadir más si es necesario
                }
            },


            { "Ad Tracking and Analytics", new HashSet<string>
                {
                    "doubleclick.net", "ad.doubleclick.net", "googleadservices.com", "googlesyndication.com",
                    "google-analytics.com", "analytics.google.com", "facebook.com", "fbcdn.net",
                    "omtrdc.net", "2o7.net", "criteo.com", "taboola.com", "outbrain.com",
                    "ads-twitter.com", "amazon-adsystem.com", "media.net", "adnxs.com",
                    "pubmatic.com", "adsrvr.org", "quantserve.com", "quantcast.com",
                    "liveramp.com", "bluekai.com", "scorecardresearch.com", "adroll.com",
                    "yahoo.com", "ads.yahoo.com", "bing.com", "ads.microsoft.com",
                    "moat.com", "crwdcntrl.net", "adform.net", "flurry.com",
                    "indexexchange.com", "xandr.com", "appnexus.com", "tealiumiq.com",
                    "tealium.com", "sizmek.com", "verizonmedia.com", "aol.com"
                    // Añadir más si es necesario
                }
            },


            {"Online Office Suite", new HashSet<string>
                {
                    "office.com", "login.microsoftonline.com", "outlook.com", // Microsoft Office y servicios relacionados
                    "docs.google.com", "drive.google.com", "sheets.google.com", "slides.google.com", // Google Docs, Sheets, Slides, y Drive
                    "zoho.com", "writer.zoho.com", "sheet.zoho.com", "show.zoho.com", // Zoho Office Suite
                    "icloud.com", "pages.icloud.com", "numbers.icloud.com", "keynote.icloud.com", // Apple iCloud (Pages, Numbers, Keynote)
                    "onlyoffice.com", "docs.onlyoffice.com", // ONLYOFFICE
                    "dropbox.com", "paper.dropbox.com", // Dropbox Paper
                    "quip.com", // Salesforce Quip
                    "notion.so", // Notion, aunque es más versátil, se usa mucho para la creación y edición de documentos
                    "slite.com", // Slite, herramienta colaborativa para documentos
                    "confluence.com", // Atlassian Confluence, herramienta para documentos colaborativos en equipos
                    "etherpad.org", // Etherpad, editor de documentos colaborativo de código abierto
                    "polarisoffice.com", // Polaris Office, una suite de ofimática online
                    "writelatex.com", "overleaf.com", // Overleaf, plataforma colaborativa para LaTeX
                    "thinkfree.com", // ThinkFree Office
                    "zoho.eu", "zoho.in", // Otros dominios de Zoho para diferentes regiones
                    "libreoffice.org", "collaboraoffice.com", // LibreOffice y Collabora Online
                    "monday.com", // Aunque es una herramienta de gestión de proyectos, incluye capacidades de documentos colaborativos
                    "evernote.com", // Evernote, utilizado para notas, pero también para documentos en línea
                    "onenote.com", // Microsoft OneNote, parte de la suite Office, orientado a notas y documentos
                    "box.com", "box.net", // Box, plataforma de almacenamiento y colaboración, incluye editores de documentos
                    "cryptpad.fr", // CryptPad, suite de ofimática colaborativa cifrada
                    "hackmd.io", "codimd.org", // HackMD y CodiMD, plataformas para la edición colaborativa de documentos en Markdown
                    "onlyoffice.eu", // Dominio europeo de ONLYOFFICE
                    // Añadir más si es necesario
                }
            },



            { "Banking", new HashSet<string>
                {
                    // América del Norte
                    "bankofamerica.com", "chase.com", "wellsfargo.com", "citi.com", "usbank.com",
                    "pnc.com", "capitalone.com", "td.com", "bbva.com", "suntrust.com",
                    "bmo.com", "ally.com", "citizensbank.com", "hsbc.com", "scotiabank.com",
                    "bbt.com", "regions.com", "huntington.com", "mtb.com", "key.com",
                    "fifththirdbank.com", "firstcitizens.com", "synchronybank.com", "firstrepublic.com", "bankofthewest.com",
                    "zionsbank.com", "cibc.com", "desjardins.com", "nationalbank.ca", "bancomer.com",
    
                    // América Latina
                    "banamex.com", "banorte.com", "hsbc.com.mx", "santander.com.mx", "scotiabank.com.mx",
                    "itau.com.br", "bradesco.com.br", "bancodobrasil.com.br", "santander.com.br", "caixa.gov.br",
                    "banreservas.com", "bci.cl", "corpbanca.cl", "bancochile.cl", "davivienda.com",
                    "bancodebogota.com", "bancolombia.com", "bbva.pe", "interbank.com.pe", "bcp.com.pe",
                    "bancoestado.cl", "bancopatagonia.com.ar", "macro.com.ar", "galicia.com.ar", "bbva.com.ar",

                    // Europa
                    "barclays.com", "santander.com", "lloydsbank.com", "natwest.com", "rbs.com",
                    "hsbc.co.uk", "standardchartered.com", "deutsche-bank.de", "commerzbank.de", "bnpparibas.com",
                    "societegenerale.com", "unicreditgroup.eu", "intesa.it", "ing.com", "rabobank.com",
                    "abnamro.com", "credit-suisse.com", "caixabank.com", "sabadell.com", "bankinter.com",
                    "swedbank.com", "sebgroup.com", "danskebank.com", "nordea.com", "op.fi",
                    "shb.se", "bnp.com", "ubs.com", "bnp.com", "clydesdalebank.co.uk", 

                    // Asia
                    "dbs.com", "ocbc.com", "uobgroup.com", "hsbc.com.hk", "citibank.com.hk",
                    "bankofchina.com", "icbc.com.cn", "chinaconstructionbank.com", "agriculturalbank.com", "bankcomm.com",
                    "smbc.co.jp", "mufg.jp", "mizuhobank.com", "ocbc.com.sg", "uob.com.sg",
                    "maybank2u.com.my", "cimbclicks.com.my", "rhbgroup.com", "hsbc.co.in", "hdfcbank.com",
                    "icicibank.com", "axisbank.com", "kotak.com", "dbs.com.sg", "posb.com.sg",

                    // Australia y Oceanía
                    "anz.com", "westpac.com.au", "nab.com.au", "commbank.com.au", "rbnz.govt.nz",
                    "asb.co.nz", "kiwibank.co.nz", "bankofmelbourne.com.au", "bankwest.com.au", "bendigo.com.au",
                    "stgeorge.com.au", "suncorp.com.au", "macquarie.com.au", "amp.com.au", "bnz.co.nz",
                    "boq.com.au", "teachersmutualbank.com.au", "creditunionaustralia.com.au", "mebank.com.au", "ubank.com.au",

                    // África
                    "standardbank.co.za", "absa.co.za", "fnb.co.za", "nedbank.co.za", "capitecbank.co.za",
                    "equitybank.co.ke", "kcbgroup.com", "co-opbank.co.ke", "ecobank.com", "stanbicbank.co.ug",
                    "gtbank.com", "zenithbank.com", "accessbankplc.com", "uba.com", "firstbanknigeria.com",
                    "barclaysafrica.com", "capitecbank.co.za", "absa.africa", "nbs.mw", "fbnholdings.com",
                    "ecobank.com", "crdbbank.co.tz", "bdo.com.ph", "metrobank.com.ph", "bpi.com.ph"
                }
            },

            { "AI", new HashSet<string>
                {
                    "chatgpt.com",  "openai.com", "deepmind.com", "huggingface.co", "stability.ai", "anthropic.com",
                    "cohere.ai", "runwayml.com", "abacus.ai", "c3.ai", "senseTime.com",
                    "petuum.com", "clarifai.com", "synthesis.ai", "h2o.ai", "anyscale.com",
                    "vicarious.com", "pathmind.com", "cogitocorp.com", "xnor.ai", "insitro.com",
                    "affectiva.com", "primer.ai", "algolux.com", "rekognition.com", "neuralink.com",
                    "skymind.global", "sift.com", "dataiku.com", "descript.com", "ubiquity6.com",
                    "anagog.com", "kneron.com", "onfido.com", "people.ai", "gradio.app",
                    "indico.io", "cloudminds.com", "deepvision.io", "neon.life", "deepgram.com",
                    "symphonyai.com", "algorithmiq.com", "v7labs.com", "ai21.com", "scribehow.com",
                    "sentient.io", "brew.com", "labelfuse.com", "soundhound.com", "ai-dentify.com",
                    "turing.com", "replicate.com", "pienso.com", "brainchip.com.au", "run.ai",
                    "scale.ai", "deepai.org", "rosette.com", "aseemble.com", "aiethic.com",
                    "brytlyt.com", "botpress.com", "akool.com", "viz.ai", "rembrand.ai",
                    "luminovo.ai", "teneo.ai", "clarifai.com", "voicery.com", "knime.com",
                    "spell.ml", "komodohealth.com", "cloudfactory.com", "drift.com", "realeyesit.com",
                    "skyl.ai", "verbit.ai", "sentinelone.com", "saama.com", "babylonhealth.com",
                    "merantix.com", "activeloop.ai", "applyflow.com", "hasty.ai", "abbys.ai"
                }
            },

            { "Technical Forums", new HashSet<string>
                    {
                    "stackoverflow.com", "stackexchange.com", "askubuntu.com", "superuser.com", "serverfault.com",
                    "codeproject.com", "codesignal.com", "dev.to", "dzone.com", "github.community",
                    "reddit.com", "sitepoint.com", "codenewbie.org", "exercism.io", "codecademy.com",
                    "geeksforgeeks.org", "hackerrank.com", "leetcode.com", "topcoder.com", "quora.com",
                    "byte-by-byte.com", "kaggle.com", "devhub.io", "devrant.com", "softwareengineering.stackexchange.com",
                    "cs.stackexchange.com", "programmersforum.ru", "programmers.stackexchange.com", "sololearn.com", "stackshare.io",
                    "hackforums.net", "codementor.io", "devshed.com", "php.net", "perlmonks.org",
                    "java-forums.org", "codingforums.com", "rosettacode.org", "toptal.com", "codereview.stackexchange.com",
                    "webmasters.stackexchange.com", "gamedev.net", "gamedev.stackexchange.com", "android.stackexchange.com", "blender.stackexchange.com",
                    "unix.stackexchange.com", "security.stackexchange.com", "softwareengineeringdaily.com", "python-forum.io", "laracasts.com",
                    "xamarin.com", "dotnetkicks.com", "programmersheaven.com", "cplusplus.com", "javaworld.com",
                    "oracle.com", "spigotmc.org", "minecraftforum.net", "vbforums.com", "dreamincode.net",
                    "thecodingforums.com", "codinghorror.com", "turing.com", "codeforces.com", "cppreference.com",
                    "w3schools.com", "html5rocks.com", "geekinterview.com", "careerkarma.com", "techgig.com",
                    "hackerearth.com", "overclock.net", "smashingmagazine.com", "linuxquestions.org", "ubuntuforums.org",
                    "daniweb.com", "perldoc.perl.org", "stackoverflow.blog", "civicrm.org", "freelancer.com",
                    "peopleperhour.com", "guru.com", "upwork.com", "weworkremotely.com", "remoteok.com",
                    "dribbble.com", "behance.net", "99designs.com", "topcoder.com", "toptal.com",
                    "producthunt.com", "sideprojectors.com", "hackernoon.com", "indiehackers.com", "hashnode.com",
                    "devdojo.com", "gitter.im", "discord.com", "slack.com", "mattermost.com",
                    "clubhouse.io", "taskade.com", "clickup.com", "asana.com", "trello.com",
                    "jira.com", "bitbucket.org", "gitlab.com", "sourceforge.net", "savannah.gnu.org",
                    "repo.or.cz", "gitkraken.com", "git-scm.com", "bazaar.canonical.com", "mercurial-scm.org",
                    "phabricator.org", "gitea.io", "gogs.io", "pagure.io", "forge.pallavi.sh",
                    "repl.it", "glitch.com", "codepen.io", "jsfiddle.net", "jsbin.com",
                    "c9.io", "codio.com", "cloud9.com", "koding.com", "codeanywhere.com",
                    "ideone.com", "collabedit.com", "overleaf.com", "vscodium.com", "playcode.io",
                    "codesandbox.io", "repl.it", "glitch.com", "hackmd.io", "pastebin.com",
                    "termux.com", "iterm2.com", "alacritty.org", "kitty.app", "hyper.is",
                    "cmder.net", "fishshell.com", "zsh.org", "bash.org", "xon.sh"
                    }
            },

           { "Gaming Platforms", new HashSet<string>
            {
                "store.steampowered.com", "epicgames.com", "xbox.com", "store.playstation.com", "nintendo.com",
                "gog.com", "ubisoft.com", "ea.com", "rockstargames.com", "battle.net",
                "store.riotgames.com", "greenmangaming.com", "humblebundle.com", "itch.io", "origin.com",
                "paradoxplaza.com", "gamesplanet.com", "fanatical.com"
            } },

            { "Adult Content Sites", new HashSet<string>
                {
                    "pornhub.com", "xvideos.com", "xvideos.es", "chaturbate.com", "redtube.com",
                    "youporngay.com", "youporn.com", "xhamster.com", "pornhd.com", "porn.com",
                    "spankbang.com", "tube8.com", "pornrocket.com", "brazzers.com", "naughtyamerica.com",
                    "hardx.com", "bangbros.com", "realitykings.com", "faketaxi.com", "mofos.com",
                    "teamskeet.com", "evilangel.com", "metart.com", "bellesa.co", "kink.com"
                } },


           { "Cryptocurrency Platforms", new HashSet<string>
                {
                    "binance.com", "coinbase.com", "kraken.com", "crypto.com", "gemini.com",
                    "bittrex.com", "bitstamp.net", "bitfinex.com", "okx.com", "huobi.com",
                    "gate.io", "kucoin.com", "bybit.com", "mexc.com", "phemex.com",
                    "deribit.com", "uniswap.org", "pancakeswap.finance", "sushiswap.com", "dydx.exchange",
                    "aave.com", "compound.finance", "makerdao.com", "blockchain.com", "trustwallet.com",
                    "ledger.com", "trezor.io"
                } },


            { "Hacking and Cybersecurity Sites", new HashSet<string>
                {
                    "exploit-db.com", "hackthebox.com", "vulnhub.com", "kali.org", "blackarch.org",
                    "offensive-security.com", "root-me.org", "tryhackme.com", "packetstormsecurity.com", "nmap.org",
                    "metasploit.com", "shodan.io", "rapid7.com", "cybercrime-tracker.net", "securityfocus.com",
                    "wireshark.org", "aircrack-ng.org", "hashcat.net", "openwall.com", "netsecfocus.com",
                    "owasp.org", "bugcrowd.com", "hackerone.com", "exploitwarez.com", "greysec.net",
                    "underground-forum.com", "darknet.org.uk", "hackforums.net"
                } },


            { "Other", new HashSet<string>() } // Otros dominios no categorizados
        };





    }
}
