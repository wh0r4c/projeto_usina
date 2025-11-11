document.addEventListener('DOMContentLoaded', () => {

    let API_BASE_URL;

    if (window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1') {
        //Aqui estamos no Local
        API_BASE_URL = 'http://localhost:5104';
    } else {
        //Estamos em Produção
        API_BASE_URL = 'https://projeto-usina.onrender.com';
    }

    let currentToken = null;
    let textoParaFalar = '';

    // --- SELETORES DE ELEMENTOS (Telas) ---
    const telaLogin = document.getElementById('tela-login');
    const telaPrincipal = document.getElementById('tela-principal');
    const loginForm = document.getElementById('login-form');
    const inputCpf = document.getElementById('cpf');
    const inputPin = document.getElementById('pin');
    const loginError = document.getElementById('login-error');
    const saudacao = document.getElementById('saudacao');
    const btnSair = document.getElementById('btn-sair');
    const cpfLimpo = inputCpf.value.replace(/\D/g, '');

    // --- INICIALIZAÇÃO DOS MODAIS BOOTSTRAP ---

    // Holerite
    const modalHoleriteEl = document.getElementById('tela-holerite-detalhe');
    const bsModalHolerite = new bootstrap.Modal(modalHoleriteEl);
    const btnHolerite = document.getElementById('btn-holerite');
    const btnFecharHolerite = document.getElementById('btn-fechar-holerite');
    const valorHolerite = document.getElementById('holerite-valor-liquido');
    const btnOuvirHolerite = document.getElementById('btn-ouvir-holerite');
    const btnBaixarPdf = document.getElementById('btn-baixar-pdf');
    const holeriteError = document.getElementById('holerite-error');

    // Falar com RH
    const modalRhEl = document.getElementById('tela-rh-gravar');
    const bsModalRh = new bootstrap.Modal(modalRhEl);
    const btnFalarRh = document.getElementById('btn-falar-rh');
    const btnFecharRh = document.getElementById('btn-fechar-rh');
    const btnGravarAudio = document.getElementById('btn-gravar-audio');
    const btnPararAudio = document.getElementById('btn-parar-audio');
    const btnEnviarAudio = document.getElementById('btn-enviar-audio');
    const audioPreview = document.getElementById('audio-preview');
    const rhStatus = document.getElementById('rh-status');
    let mediaRecorder, audioChunks = [], audioBlob = null;

    // Avisos
    const modalAvisosEl = document.getElementById('tela-avisos');
    const bsModalAvisos = new bootstrap.Modal(modalAvisosEl);
    const btnAvisos = document.getElementById('btn-avisos');
    const btnFecharAvisos = document.getElementById('btn-fechar-avisos');
    const listaAvisosContainer = document.getElementById('lista-avisos-container');
    const avisosStatus = document.getElementById('avisos-status');

    // FAQ
    const modalFaqEl = document.getElementById('tela-faq');
    const bsModalFaq = new bootstrap.Modal(modalFaqEl);
    const btnFaq = document.getElementById('btn-faq');
    const btnFecharFaq = document.getElementById('btn-fechar-faq');
    const listaFaqContainer = document.getElementById('lista-faq-container');
    const faqStatus = document.getElementById('faq-status');

    // Banco de Horas
    const modalBancoHorasEl = document.getElementById('tela-banco-horas');
    const bsModalBancoHoras = new bootstrap.Modal(modalBancoHorasEl);
    const btnBancoHoras = document.getElementById('btn-banco-horas');
    const btnFecharBancoHoras = document.getElementById('btn-fechar-banco-horas');
    const bancoHorasValor = document.getElementById('banco-horas-valor');
    const bancoHorasData = document.getElementById('banco-horas-data');
    const btnOuvirBancoHoras = document.getElementById('btn-ouvir-banco-horas');
    const bancoHorasStatus = document.getElementById('banco-horas-status');

    // Férias
    const modalFeriasEl = document.getElementById('tela-ferias');
    const bsModalFerias = new bootstrap.Modal(modalFeriasEl);
    const btnFerias = document.getElementById('btn-ferias');
    const btnFecharFerias = document.getElementById('btn-fechar-ferias');
    const feriasStatus = document.getElementById('ferias-status');
    const feriasData = document.getElementById('ferias-data');
    const feriasSaldoDias = document.getElementById('ferias-saldo-dias');
    const btnOuvirFerias = document.getElementById('btn-ouvir-ferias');
    const feriasMsgStatus = document.getElementById('ferias-msg-status');

    // ** NOVO MODAL (v7) **
    const modalDefinirPinEl = document.getElementById('tela-definir-pin');
    const bsModalDefinirPin = new bootstrap.Modal(modalDefinirPinEl, { backdrop: 'static', keyboard: false });
    const definirPinForm = document.getElementById('definir-pin-form');
    const novoPin1 = document.getElementById('novo-pin-1');
    const novoPin2 = document.getElementById('novo-pin-2');
    const definirPinStatus = document.getElementById('definir-pin-status');


    // --- FUNÇÕES DE LÓGICA ---

    function mostrarTela(telaParaMostrar) {
        // 1. Esconde todas as "telas" principais
        telaLogin.style.display = 'none';
        telaPrincipal.style.display = 'none';

        // 2. Mostra APENAS a que foi pedida
        telaParaMostrar.style.display = 'block';
    }

    async function fazerLogin(e) {
        e.preventDefault();
        loginError.textContent = '';

        try {
            const resposta = await fetch(`${API_BASE_URL}/api/auth/login`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    cpf: cpfLimpo,
                    pin: inputPin.value // Isto é o PIN ou a Matrícula
                })
            });
            if (!resposta.ok) throw new Error('CPF ou PIN/Matrícula inválidos.');

            const dados = await resposta.json();

            // Salva o token e o nome imediatamente
            currentToken = dados.token;
            sessionStorage.setItem('token', dados.token);
            saudacao.textContent = `Olá, ${dados.nome}!`;

            // ** AQUI ESTÁ A NOVA LÓGICA (v7) **
            if (dados.status === 'primeiro_login') {
                // É um novo utilizador, mostra o modal para definir o PIN
                bsModalDefinirPin.show();
            } else {
                // É um utilizador normal, mostra o menu principal
                mostrarTela(telaPrincipal);
            }

        } catch (err) {
            loginError.textContent = err.message;
        }
    }

    function fazerLogout() {
        currentToken = null;
        sessionStorage.removeItem('token');
        inputCpf.value = '';
        inputPin.value = '';
        mostrarTela(telaLogin);
        // Recarrega a página para garantir que tudo é limpo
        window.location.reload();
    }

    // ** NOVA FUNÇÃO (v7) **
    async function definirNovoPin(e) {
        e.preventDefault();
        definirPinStatus.textContent = '';

        // 1. Validação do Frontend
        if (novoPin1.value.length !== 4) {
            definirPinStatus.textContent = 'O PIN deve ter exatamente 4 dígitos.';
            return;
        }
        if (novoPin1.value !== novoPin2.value) {
            definirPinStatus.textContent = 'Os PINs não coincidem. Tente novamente.';
            return;
        }
        if (!currentToken) {
            definirPinStatus.textContent = 'Erro de autenticação. Tente fazer o login novamente.';
            return;
        }

        try {
            // 2. Enviar o novo PIN para o backend
            const resposta = await fetch(`${API_BASE_URL}/api/auth/definir-pin`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${currentToken}` // Envia o token do "primeiro_login"
                },
                body: JSON.stringify({
                    novoPin: novoPin1.value
                })
            });

            if (!resposta.ok) {
                const erro = await resposta.json();
                throw new Error(erro.message || 'Não foi possível definir o PIN.');
            }

            // 3. Sucesso!
            bsModalDefinirPin.hide(); // Esconde o modal de PIN
            mostrarTela(telaPrincipal); // Mostra o menu principal

        } catch (err) {
            definirPinStatus.textContent = err.message;
        }
    }


    // --- MÓDULO HOLERITE ---
    async function carregarHolerite(e) {
        e.preventDefault(); // Previne o link de navegar
        holeriteError.textContent = '';
        btnBaixarPdf.style.display = 'none';

        if (!currentToken) {
            holeriteError.textContent = 'Erro de autenticação.';
            return;
        }

        bsModalHolerite.show(); // Mostra o modal Bootstrap

        try {
            const resposta = await fetch(`${API_BASE_URL}/api/holerite`, {
                method: 'GET',
                headers: { 'Authorization': `Bearer ${currentToken}` }
            });
            if (!resposta.ok) throw new Error('Não foi possível carregar.');

            const dados = await resposta.json();
            const valorFormatado = dados.valor_liquido.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });

            valorHolerite.textContent = valorFormatado;
            textoParaFalar = dados.texto_para_fala;
            if (dados.tem_pdf) {
                btnBaixarPdf.style.display = 'block';
            }
        } catch (err) {
            holeriteError.textContent = err.message;
        }
    }

    async function baixarPdf() {
        // ... (código do baixarPdf não muda)
        try {
            const resposta = await fetch(`${API_BASE_URL}/api/holerite/pdf`, {
                method: 'GET',
                headers: { 'Authorization': `Bearer ${currentToken}` }
            });
            if (!resposta.ok) throw new Error('Não foi possível baixar o PDF.');

            const blob = await resposta.blob();
            const contentDisposition = resposta.headers.get('content-disposition');
            let filename = 'holerite.pdf';
            if (contentDisposition) {
                const match = contentDisposition.match(/filename="?([^"]+)"?/);
                if (match) filename = match[1];
            }

            const url = window.URL.createObjectURL(blob);
            const a = document.createElement('a');
            a.style.display = 'none'; a.href = url; a.download = filename;
            document.body.appendChild(a);
            a.click();
            window.URL.revokeObjectURL(url); a.remove();
        } catch (err) {
            holeriteError.textContent = err.message;
        }
    }

    // --- MÓDULO RH ---
    function abrirModalRh(e) {
        e.preventDefault();
        rhStatus.textContent = '';
        audioPreview.style.display = 'none';
        audioPreview.src = '';
        btnEnviarAudio.style.display = 'none';
        btnGravarAudio.style.display = 'block';
        btnPararAudio.style.display = 'none';
        btnGravarAudio.disabled = false;
        audioChunks = [];
        audioBlob = null;
        bsModalRh.show(); // MUDANÇA
    }

    async function iniciarGravacao() {
        // ... (código do iniciarGravacao não muda)
        rhStatus.textContent = 'Pedindo permissão...';
        try {
            const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
            mediaRecorder = new MediaRecorder(stream);
            mediaRecorder.ondataavailable = (event) => audioChunks.push(event.data);

            mediaRecorder.onstop = () => {
                audioBlob = new Blob(audioChunks, { type: 'audio/wav' });
                const audioUrl = URL.createObjectURL(audioBlob);
                audioPreview.src = audioUrl;
                audioPreview.style.display = 'block';
                btnEnviarAudio.style.display = 'block';
                btnEnviarAudio.disabled = false;
            };

            audioChunks = [];
            btnGravarAudio.style.display = 'none';
            btnPararAudio.style.display = 'block';
            rhStatus.textContent = 'Gravando... 🔴';
            mediaRecorder.start();
        } catch (err) {
            console.error('Erro ao acessar microfone:', err);
            rhStatus.textContent = 'Erro: Não foi possível acessar o microfone.';
        }
    }

    function pararGravacao() {
        // ... (código do pararGravacao não muda)
        if (mediaRecorder) {
            mediaRecorder.stop();
            mediaRecorder.stream.getTracks().forEach(track => track.stop());
            btnPararAudio.style.display = 'none';
            btnGravarAudio.style.display = 'block';
            rhStatus.textContent = 'Gravação parada. Ouça e envie.';
        }
    }

    async function enviarGravacao() {
        // ... (código do enviarGravacao não muda)
        if (!audioBlob || !currentToken) {
            rhStatus.textContent = 'Nenhum áudio ou erro de login.';
            return;
        }
        rhStatus.textContent = 'Enviando...';
        btnEnviarAudio.disabled = true;
        const formData = new FormData();
        formData.append('audioFile', audioBlob, 'gravacao.wav');

        try {
            const resposta = await fetch(`${API_BASE_URL}/api/suporte/audio`, {
                method: 'POST',
                headers: { 'Authorization': `Bearer ${currentToken}` },
                body: formData
            });
            const dados = await resposta.json();
            if (!resposta.ok) throw new Error(dados.message || 'Erro ao enviar.');
            rhStatus.textContent = `Enviado! Protocolo: ${dados.ticketId}`;
            btnEnviarAudio.disabled = true;
        } catch (err) {
            rhStatus.textContent = `Erro: ${err.message}`;
            btnEnviarAudio.disabled = false;
        }
    }

    // --- MÓDULO AVISOS ---
    async function carregarAvisos(e) {
        e.preventDefault();
        avisosStatus.textContent = 'Carregando avisos...';
        listaAvisosContainer.innerHTML = '';
        if (!currentToken) {
            avisosStatus.textContent = 'Erro de autenticação.';
            return;
        }
        bsModalAvisos.show(); // MUDANÇA

        try {
            const resposta = await fetch(`${API_BASE_URL}/api/avisos`, {
                method: 'GET',
                headers: { 'Authorization': `Bearer ${currentToken}` }
            });
            if (!resposta.ok) throw new Error('Não foi possível carregar os avisos.');

            const avisos = await resposta.json();
            if (avisos.length === 0) {
                avisosStatus.textContent = 'Nenhum aviso no momento.';
                return;
            }
            avisosStatus.textContent = '';

            avisos.forEach(aviso => {
                // Re-cria os cards com classes do Bootstrap
                const card = document.createElement('div');
                card.className = 'card mb-3';
                card.innerHTML = `
                    <div class="card-body">
                        <h5 class="card-title text-primary">${aviso.titulo}</h5>
                        <p class="card-text">${aviso.conteudo}</p>
                        <div class="d-flex justify-content-between align-items-center">
                            <small class="text-muted">Publicado em: ${aviso.data}</small>
                            <button class="btn btn-sm btn-outline-primary btn-ouvir-aviso">▶️ Ouvir</button>
                        </div>
                    </div>
                `;
                // Adiciona o listener no botão recém-criado
                card.querySelector('.btn-ouvir-aviso').onclick = () => {
                    falarTexto(aviso.textoParaFala);
                };
                listaAvisosContainer.appendChild(card);
            });
        } catch (err) {
            avisosStatus.textContent = `Erro: ${err.message}`;
        }
    }

    // --- MÓDULO FAQ ---
    async function carregarFaq(e) {
        e.preventDefault();
        faqStatus.textContent = 'Carregando dúvidas...';
        listaFaqContainer.innerHTML = '';
        bsModalFaq.show(); // MUDANÇA

        try {
            const resposta = await fetch(`${API_BASE_URL}/api/faq`, { method: 'GET' });
            if (!resposta.ok) throw new Error('Não foi possível carregar as dúvidas.');

            const faqs = await resposta.json();
            if (faqs.length === 0) {
                faqStatus.textContent = 'Nenhuma dúvida cadastrada.';
                return;
            }
            faqStatus.textContent = '';

            faqs.forEach(faq => {
                // Re-cria os cards com classes do Bootstrap
                const card = document.createElement('div');
                card.className = 'card faq-card mb-2'; // Usamos a classe customizada

                card.innerHTML = `
                    <div class="faq-pergunta">
                        <h4 class="mb-0">${faq.pergunta}</h4>
                        <span class="fs-5">▼</span>
                    </div>
                    <div class="faq-resposta">
                        <p>${faq.resposta}</p>
                        <button class="btn btn-sm btn-outline-primary btn-ouvir-faq">▶️ Ouvir Resposta</button>
                    </div>
                `;

                card.querySelector('.btn-ouvir-faq').onclick = () => {
                    falarTexto(faq.textoParaFala);
                };

                card.querySelector('.faq-pergunta').addEventListener('click', () => {
                    document.querySelectorAll('.faq-card.active').forEach(item => {
                        if (item !== card) {
                            item.classList.remove('active');
                            item.querySelector('.faq-pergunta span').innerText = '▼';
                        }
                    });
                    card.classList.toggle('active');
                    if (card.classList.contains('active')) {
                        card.querySelector('.faq-pergunta span').innerText = '▲';
                    } else {
                        card.querySelector('.faq-pergunta span').innerText = '▼';
                        window.speechSynthesis.cancel();
                    }
                });
                listaFaqContainer.appendChild(card);
            });
        } catch (err) {
            faqStatus.textContent = `Erro: ${err.message}`;
        }
    }

    // --- MÓDULO BANCO DE HORAS ---
    async function carregarBancoHoras(e) {
        e.preventDefault();
        bancoHorasStatus.textContent = 'Carregando saldo...';
        if (!currentToken) {
            bancoHorasStatus.textContent = 'Erro de autenticação.';
            return;
        }
        bsModalBancoHoras.show(); // MUDANÇA

        try {
            const resposta = await fetch(`${API_BASE_URL}/api/bancohoras`, {
                method: 'GET',
                headers: { 'Authorization': `Bearer ${currentToken}` }
            });
            if (!resposta.ok) throw new Error('Não foi possível carregar o saldo.');

            const dados = await resposta.json();
            bancoHorasStatus.textContent = '';
            bancoHorasValor.textContent = dados.horasFormatadas;
            bancoHorasData.textContent = `Atualizado em: ${dados.dataAtualizacao}`;
            btnOuvirBancoHoras.onclick = () => falarTexto(dados.textoParaFala);
        } catch (err) {
            bancoHorasStatus.textContent = `Erro: ${err.message}`;
        }
    }

    // --- MÓDULO FÉRIAS ---
    async function carregarFerias(e) {
        e.preventDefault();
        feriasMsgStatus.textContent = 'Carregando dados...';
        if (!currentToken) {
            feriasMsgStatus.textContent = 'Erro de autenticação.';
            return;
        }
        bsModalFerias.show(); // MUDANÇA

        try {
            const resposta = await fetch(`${API_BASE_URL}/api/ferias`, {
                method: 'GET',
                headers: { 'Authorization': `Bearer ${currentToken}` }
            });
            if (!resposta.ok) throw new Error('Não foi possível carregar os dados.');

            const dados = await resposta.json();
            feriasMsgStatus.textContent = '';
            feriasStatus.textContent = dados.status;
            feriasData.textContent = dados.dataProgramada;
            feriasSaldoDias.textContent = `Saldo: ${dados.diasDeSaldo}`;
            btnOuvirFerias.onclick = () => falarTexto(dados.textoParaFala);
        } catch (err) {
            feriasMsgStatus.textContent = `Erro: ${err.message}`;
        }
    }

    // --- FUNÇÃO DE FALA (Genérica) ---
    function falarTexto(texto) {
        window.speechSynthesis.cancel();
        if ('speechSynthesis' in window && texto) {
            const synth = window.speechSynthesis;
            const utterance = new SpeechSynthesisUtterance(texto);
            utterance.lang = 'pt-BR';
            synth.speak(utterance);
        } else {
            alert('Seu navegador não suporta a função de voz.');
        }
    }


    // --- REGISTRO DE EVENTOS (Listeners) ---
    loginForm.addEventListener('submit', fazerLogin);
    btnSair.addEventListener('click', fazerLogout);

    // ** NOVO LISTENER (v7) **
    definirPinForm.addEventListener('submit', definirNovoPin);

    // Botões do Menu
    btnHolerite.addEventListener('click', carregarHolerite);
    btnFalarRh.addEventListener('click', abrirModalRh);
    btnAvisos.addEventListener('click', carregarAvisos);
    btnFaq.addEventListener('click', carregarFaq);
    btnBancoHoras.addEventListener('click', carregarBancoHoras);
    btnFerias.addEventListener('click', carregarFerias);

    // Botões dos Modais
    btnOuvirHolerite.addEventListener('click', () => falarTexto(textoParaFalar));
    btnBaixarPdf.addEventListener('click', baixarPdf);
    btnGravarAudio.addEventListener('click', iniciarGravacao);
    btnPararAudio.addEventListener('click', pararGravacao);
    btnEnviarAudio.addEventListener('click', enviarGravacao);

    // Eventos de Fechar (para parar a fala)
    const allModals = [modalHoleriteEl, modalRhEl, modalAvisosEl, modalFaqEl, modalBancoHorasEl, modalFeriasEl];
    allModals.forEach(modal => {
        modal.addEventListener('hidden.bs.modal', () => {
            window.speechSynthesis.cancel(); // Para a fala quando qualquer modal fechar
        });
    });

    // --- INICIALIZAÇÃO ---
    const tokenSalvo = sessionStorage.getItem('token');
    if (tokenSalvo) {
        // Esta lógica mudou. Não podemos mais assumir que temos o nome
        // ou que podemos mostrar a tela principal.
        // Vamos simplificar e apenas guardar o token.
        // Se o token for de um "primeiro_login", o backend vai 
        // dar erro 401 nos endpoints de qualquer forma, 
        // forçando o utilizador a fazer login de novo.
        currentToken = tokenSalvo;
        // Vamos verificar se o token ainda é válido (de forma simples)
        // Se o utilizador já fez login antes, mostramos a tela.
        // Esta parte da lógica pode ser melhorada, mas por agora
        // vamos assumir que se o token existe, é de um login normal.

        // Vamos mudar esta lógica:
        mostrarTela(telaLogin); // Sempre começa no login
        // Se o tokenSalvo existir, o logout fará mais sentido
    } else {
        mostrarTela(telaLogin);
    }
});