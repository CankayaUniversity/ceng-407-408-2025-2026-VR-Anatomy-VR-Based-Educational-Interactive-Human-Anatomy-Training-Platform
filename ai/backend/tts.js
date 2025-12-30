let isSpeaking = false;
let currentUtterance = null;


let cachedVoices = [];
window.speechSynthesis.onvoiceschanged = () => {
  cachedVoices = window.speechSynthesis.getVoices();
};

function pickTurkishVoice(preferMale = true) {
  const voices = cachedVoices.length ? cachedVoices : window.speechSynthesis.getVoices();
  const lowered = (v) => v.name.toLowerCase();

  const maleHints = ["cem", "arif", "mehmet", "murat", "emir", "onur", "eren", "male", "erkek"];
  const femaleHints = ["filiz", "female", "kadın"];

  const matchByHints = (hints) => voices.find(v =>
    v.lang === "tr-TR" &&
    hints.some(h => lowered(v).includes(h))
  );

  if (preferMale) {
    const male = matchByHints(maleHints);
    if (male) return male;
  }

  
  const female = matchByHints(femaleHints);
  if (female) return female;

 
  const turkish = voices.find(v => v.lang === "tr-TR");
  if (turkish) return turkish;


  return voices.find(v => v.lang?.startsWith("tr")) || null;
}

function speakText(text) {
  if (!("speechSynthesis" in window)) {
    console.warn("Tarayıcı Text-to-Speech desteklemiyor");
    return;
  }

  window.speechSynthesis.cancel();

  const utterance = new SpeechSynthesisUtterance(text);
  utterance.lang = "tr-TR";
  utterance.rate = 1.0;  
  utterance.pitch = 0.98; 
  utterance.volume = 1.0;

  utterance.onstart = () => {
    isSpeaking = true;
    currentUtterance = utterance;
  };

  utterance.onend = () => {
    isSpeaking = false;
    currentUtterance = null;
  };

  const voice = pickTurkishVoice(true);
  if (voice) {
    utterance.voice = voice;
  } else {
    console.warn("Türkçe erkek sesi bulunamadı, varsayılan kullanılacak.");
  }

  window.speechSynthesis.speak(utterance);
}

function stopSpeaking() {
  if ("speechSynthesis" in window) {
    window.speechSynthesis.cancel();
    isSpeaking = false;
    currentUtterance = null;
  }
}


window.addEventListener("beforeunload", () => {
  window.speechSynthesis.cancel();
});
