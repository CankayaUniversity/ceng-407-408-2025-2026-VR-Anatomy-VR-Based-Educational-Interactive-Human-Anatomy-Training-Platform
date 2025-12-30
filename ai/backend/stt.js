let recognition = null;
let isListening = false;

function startListening(inputElement) {
  const SpeechRecognition =
    window.SpeechRecognition || window.webkitSpeechRecognition;

  if (!SpeechRecognition) {
    alert("Tarayıcınız Speech-to-Text desteklemiyor.");
    return;
  }

  if (!recognition) {
    recognition = new SpeechRecognition();
    recognition.lang = "tr-TR";
    recognition.continuous = false;
    recognition.interimResults = false;

    recognition.onresult = (event) => {
      const transcript = event.results[0][0].transcript;
      inputElement.value = transcript;
    };

    recognition.onend = () => {
      isListening = false;
      updateMicButton(false);
    };

    recognition.onerror = () => {
      isListening = false;
      updateMicButton(false);
    };
  }

  if (!isListening) {
    recognition.start();
    isListening = true;
    updateMicButton(true);
  } else {
    recognition.stop();
    isListening = false;
    updateMicButton(false);
  }
}

function updateMicButton(listening) {
  const btn = document.getElementById("micBtn");
  if (!btn) return;

  btn.textContent = listening ? "⏹️" : "🎤";
}
