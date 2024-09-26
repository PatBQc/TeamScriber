using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeamScriber
{
    public class TranslationHelper
    {
        private static readonly Dictionary<string, string> _translations;

        static TranslationHelper()
        {
            _translations = new Dictionary<string, string>
            {
                {"en", "Next, you will find the transcription of a meeting used for your questions"},
                {"es", "A continuación, encontrará la transcripción de una reunión utilizada para sus preguntas"},
                {"fr", "Ensuite, vous trouverez la transcription d'une réunion utilisée pour vos questions"},
                {"de", "Als Nächstes finden Sie die Transkription eines Meetings, das für Ihre Fragen verwendet wurde"},
                {"it", "Di seguito troverai la trascrizione di una riunione utilizzata per le tue domande"},
                {"pt", "A seguir, você encontrará a transcrição de uma reunião usada para suas perguntas"},
                {"nl", "Vervolgens vindt u de transcriptie van een vergadering die voor uw vragen is gebruikt"},
                {"ru", "Далее вы найдете стенограмму встречи, использованной для ваших вопросов"},
                {"zh", "接下来，您将找到用于回答您的问题的会议记录"},
                {"ja", "次に、あなたの質問に使用された会議の書き起こしが見つかります"},
                {"ko", "다음으로 귀하의 질문에 사용된 회의 기록을 찾으실 수 있습니다"},
                {"ar", "بعد ذلك، ستجد نصًا مكتوبًا لاجتماع تم استخدامه لأسئلتك"},
                {"hi", "इसके बाद, आप अपने प्रश्नों के लिए उपयोग की गई एक बैठक का प्रतिलेखन पाएंगे"},
                {"sv", "Härnäst hittar du transkriptionen av ett möte som använts för dina frågor"},
                {"pl", "Następnie znajdziesz transkrypcję spotkania wykorzystanego do twoich pytań"},
                {"tr", "Ardından, sorularınız için kullanılan bir toplantının dökümünü bulacaksınız"},
                {"da", "Derefter finder du transskriptionen af et møde, der er brugt til dine spørgsmål"},
                {"fi", "Seuraavaksi löydät kysymyksiisi käytetyn kokouksen litteroinnin"},
                {"no", "Deretter finner du transkripsjonen av et møte som ble brukt til spørsmålene dine"},
                {"cs", "Dále najdete přepis schůzky použité pro vaše otázky"},
                {"el", "Στη συνέχεια, θα βρείτε τη μεταγραφή μιας συνάντησης που χρησιμοποιήθηκε για τις ερωτήσεις σας"}
            };
        }

        public static string GetTranslation(string languageCode)
        {
            if (_translations.TryGetValue(languageCode.ToLower(), out string translation))
            {
                return translation;
            }
            else
            {
                throw new ArgumentException($"Translation not available for the specified language code ({languageCode}).");
            }
        }
    }
}
