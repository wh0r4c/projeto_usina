document.addEventListener('DOMContentLoaded', () => {

    // --- Lógica "Inteligente" de Ambiente ---
    let API_BASE_URL;

    if (window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1') {
        API_BASE_URL = 'http://localhost:5104';
    } else {
        API_BASE_URL = 'https://projeto-usina.onrender.com';
    }

    let adminToken = null;
    let colaboradorEditandoId = null;

    // --- Seletores de Elementos ---
    const telaLoginAdmin = document.getElementById('tela-login-admin');
    const telaDashboardAdmin = document.getElementById('tela-dashboard-admin');

    const loginFormAdmin = document.getElementById('login-form-admin');
    const adminEmail = document.getElementById('admin-email');
    const adminPassword = document.getElementById('admin-password');
    const adminLoginError = document.getElementById('admin-login-error');

    const adminSaudacao = document.getElementById('admin-saudacao');
    const btnAdminSair = document.getElementById('btn-admin-sair');
    
    // --- Seletores do Painel ---
    const painelTitulo = document.getElementById('painel-titulo');

    // Containers
    const listaColaboradoresContainer = document.getElementById('lista-colaboradores-container');
    const formColaboradorContainer = document.getElementById('form-colaborador-container');
    const listaAudiosContainer = document.getElementById('lista-audios-container');
    
    // Wrapper para alternar a visibilidade de toda a seção de colaboradores
    // (Criei este ID no HTML novo para facilitar o esconde/mostra sem perder o layout)
    const wrapperColaboradores = document.getElementById('wrapper-colaboradores');

    // Botões e Forms (Colaborador)
    const btnMostrarForm = document.getElementById('btn-mostrar-form-colaborador');
    const btnCancelarForm = document.getElementById('btn-cancelar-form-colaborador');
    const formNovoColaborador = document.getElementById('form-novo-colaborador');
    const formColaboradorStatus = document.getElementById('form-colaborador-status');

    // Navegação
    const navColaboradores = document.getElementById('nav-colaboradores');
    const navAudios = document.getElementById('nav-audios');

    // --- Seletores do Modal de Edição ---
    const modalEditarEl = document.getElementById('modal-editar-colaborador');
    const bsModalEditar = new bootstrap.Modal(modalEditarEl);
    const editarModalTitulo = document.getElementById('editar-modal-titulo');

    const formEditarBancoHoras = document.getElementById('form-editar-banco-horas');
    const statusBancoHoras = document.getElementById('status-banco-horas');

    const formEditarFerias = document.getElementById('form-editar-ferias');
    const statusFerias = document.getElementById('status-ferias');

    const formEnviarHolerite = document.getElementById('form-enviar-holerite');
    const statusHolerite = document.getElementById('status-holerite');


    // --- Funções de Lógica ---

    function mostrarTela(telaParaMostrar) {
        if (telaParaMostrar === telaDashboardAdmin) {
            telaLoginAdmin.style.display = 'none';
            // IMPORTANTE: Dashboard usa display: flex no CSS (dashboard-wrapper), 
            // mas se usarmos 'block' aqui via JS, pode quebrar o layout flex.
            // Vamos deixar o CSS controlar o layout, apenas removendo o 'none'.
            telaDashboardAdmin.style.display = 'flex'; 
            mostrarPainelColaboradores();
        } else {
            telaLoginAdmin.style.display = 'flex'; // Login também é flex
            telaDashboardAdmin.style.display = 'none';
        }
    }

    async function fazerLoginAdmin(e) {
        e.preventDefault();
        adminLoginError.textContent = 'Autenticando...';
        
        try {
            const resposta = await fetch(`${API_BASE_URL}/api/admin/login`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    email: adminEmail.value,
                    password: adminPassword.value
                })
            });
            if (!resposta.ok) throw new Error('Email ou Senha inválidos.');

            const dados = await resposta.json();
            adminToken = dados.token;
            sessionStorage.setItem('admin_token', dados.token);
            
            // Tratamento seguro caso dados.nome venha vazio
            adminSaudacao.textContent = dados.nome || 'Admin';
            
            mostrarTela(telaDashboardAdmin);
        } catch (err) {
            adminLoginError.textContent = err.message;
        }
    }

    function fazerLogoutAdmin() {
        adminToken = null;
        sessionStorage.removeItem('admin_token');
        adminEmail.value = '';
        adminPassword.value = '';
        mostrarTela(telaLoginAdmin);
    }

    // --- LÓGICA DE NAVEGAÇÃO DO PAINEL ---

    function mostrarPainelColaboradores() {
        painelTitulo.innerHTML = '<i class="bi bi-people-fill"></i> Gestão de Colaboradores';
        
        // Exibe a seção de colaboradores
        if(wrapperColaboradores) wrapperColaboradores.style.display = 'block';
        
        // Esconde a lista de áudios
        listaAudiosContainer.style.display = 'none';
        
        // Reseta visibilidade do form de cadastro (começa escondido)
        formColaboradorContainer.style.display = 'none';
        
        // Atualiza classes da Sidebar
        navColaboradores.classList.add('active');
        navColaboradores.classList.remove('text-white'); // Ajuste visual bootstrap
        navAudios.classList.remove('active');
        navAudios.classList.add('text-white');

        carregarColaboradores();
    }

    function mostrarPainelAudios() {
        painelTitulo.innerHTML = '<i class="bi bi-mic-fill"></i> Áudios Pendentes';
        
        // Esconde seção de colaboradores
        if(wrapperColaboradores) wrapperColaboradores.style.display = 'none';
        
        // Mostra lista de áudios
        listaAudiosContainer.style.display = 'block';

        // Atualiza classes da Sidebar
        navColaboradores.classList.remove('active');
        navColaboradores.classList.add('text-white');
        navAudios.classList.add('active');
        navAudios.classList.remove('text-white');

        carregarAudios();
    }

    // --- FUNÇÕES DE DADOS DO PAINEL ADMIN ---

    // 1. LISTAR COLABORADORES
    async function carregarColaboradores() {
        if (!adminToken) return;
        listaColaboradoresContainer.innerHTML = `
            <div class="text-center p-5">
                <div class="spinner-border text-primary" role="status"></div>
                <p class="mt-2 text-muted">Carregando dados...</p>
            </div>`;

        const termo = document.getElementById('filtro-termo').value;
        const dep = document.getElementById('filtro-departamento').value;

        const url = new URL(`${API_BASE_URL}/api/admin/colaboradores`);
        if (termo) url.searchParams.append('termo', termo);
        if (dep) url.searchParams.append('departamento', dep);

        try {
            const resposta = await fetch(url, {
                method: 'GET',
                headers: { 'Authorization': `Bearer ${adminToken}` }
            });

            if (!resposta.ok) throw new Error('Não foi possível carregar os colaboradores.');
            const colaboradores = await resposta.json();
            desenharTabelaColaboradores(colaboradores);
        } catch (err) {
            listaColaboradoresContainer.innerHTML = `<div class="alert alert-danger m-3">${err.message}</div>`;
        }
    }

    function desenharTabelaColaboradores(colaboradores) {
        if (colaboradores.length === 0) {
            listaColaboradoresContainer.innerHTML = '<div class="text-center p-4 text-muted">Nenhum colaborador encontrado com os filtros atuais.</div>';
            return;
        }

        let tabelaHtml = `
            <table class="table table-hover mb-0">
                <thead>
                    <tr>
                        <th class="ps-4">Nome / ID</th>
                        <th>Departamento</th>
                        <th>CPF</th>
                        <th>Matrícula</th>
                        <th>Status</th>
                        <th class="text-end pe-4">Ações</th>
                    </tr>
                </thead>
                <tbody>
        `;

        for (const colab of colaboradores) {
            const statusPin = colab.pinFoiDefinido
                ? '<span class="badge bg-success bg-opacity-10 text-success border border-success rounded-pill"><i class="bi bi-check-circle-fill"></i> Ativo</span>'
                : '<span class="badge bg-warning bg-opacity-10 text-warning border border-warning rounded-pill"><i class="bi bi-exclamation-circle-fill"></i> Pendente</span>';

            tabelaHtml += `
                <tr data-id="${colab.id}" data-nome="${colab.nome}">
                    <td class="ps-4">
                        <div class="fw-bold text-dark">${colab.nome}</div>
                        <small class="text-muted">ID: ${colab.id}</small>
                    </td>
                    <td><span class="badge bg-light text-dark border">${colab.departamento || 'Geral'}</span></td>
                    <td class="text-secondary">${colab.cpf}</td>
                    <td class="text-secondary fw-medium">${colab.matricula}</td>
                    <td>${statusPin}</td>
                    <td class="text-end pe-4">
                        <button class="btn btn-sm btn-outline-primary btn-acao btn-editar shadow-sm">
                            <i class="bi bi-pencil"></i> Editar
                        </button>
                    </td>
                </tr>
            `;
        }
        tabelaHtml += '</tbody></table>';
        listaColaboradoresContainer.innerHTML = tabelaHtml;

        document.querySelectorAll('.btn-editar').forEach(btn => {
            btn.addEventListener('click', (e) => {
                const tr = e.target.closest('tr');
                const id = tr.dataset.id;
                const nome = tr.dataset.nome;
                abrirModalEdicao(id, nome);
            });
        });
    }

    // 2. ADICIONAR COLABORADOR
    async function adicionarColaborador(e) {
        e.preventDefault();
        formColaboradorStatus.textContent = '';
        if (!adminToken) return;
        
        const btnSubmit = formNovoColaborador.querySelector('button[type="submit"]');
        const txtOriginal = btnSubmit.textContent;
        btnSubmit.disabled = true;
        btnSubmit.textContent = 'Salvando...';

        const dados = {
            nome: document.getElementById('novo-nome').value,
            departamento: document.getElementById('novo-departamento').value,
            cpf: document.getElementById('novo-cpf').value,
            matricula: document.getElementById('novo-matricula').value
        };
        try {
            const resposta = await fetch(`${API_BASE_URL}/api/admin/colaboradores`, {
                method: 'POST',
                headers: {
                    'Authorization': `Bearer ${adminToken}`,
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(dados)
            });
            if (!resposta.ok) {
                const erro = await resposta.json();
                throw new Error(erro.message || 'Erro ao criar colaborador.');
            }
            formColaboradorContainer.style.display = 'none';
            formNovoColaborador.reset();
            carregarColaboradores();
        } catch (err) {
            formColaboradorStatus.textContent = err.message;
        } finally {
            btnSubmit.disabled = false;
            btnSubmit.textContent = txtOriginal;
        }
    }

    // 3. LISTAR ÁUDIOS
    async function carregarAudios() {
        if (!adminToken) return;
        listaAudiosContainer.innerHTML = `
            <div class="text-center p-5">
                <div class="spinner-border text-primary"></div>
                <p class="mt-2 text-muted">Buscando áudios...</p>
            </div>`;
            
        try {
            const resposta = await fetch(`${API_BASE_URL}/api/admin/audios`, {
                method: 'GET',
                headers: { 'Authorization': `Bearer ${adminToken}` }
            });
            if (!resposta.ok) throw new Error('Não foi possível carregar os áudios.');
            const audios = await resposta.json();
            desenharTabelaAudios(audios);
        } catch (err) {
            listaAudiosContainer.innerHTML = `<div class="alert alert-danger">${err.message}</div>`;
        }
    }

    function desenharTabelaAudios(audios) {
        if (audios.length === 0) {
            listaAudiosContainer.innerHTML = '<div class="alert alert-success m-3"><i class="bi bi-check-all"></i> Nenhum áudio pendente.</div>';
            return;
        }
        
        let tabelaHtml = `
            <table class="table table-hover align-middle bg-white shadow-sm rounded">
                <thead class="table-light">
                    <tr>
                        <th class="ps-3">Data</th>
                        <th>Colaborador</th>
                        <th>CPF</th>
                        <th>Áudio</th>
                        <th>Status</th>
                    </tr>
                </thead>
                <tbody>
        `;
        for (const audio of audios) {
            const status = audio.resolvido
                ? '<span class="badge bg-secondary">Resolvido</span>'
                : '<span class="badge bg-danger">Pendente</span>';
            const audioUrl = `${API_BASE_URL}/${audio.caminhoArquivo}`;
            
            tabelaHtml += `
                <tr>
                    <td class="ps-3">${new Date(audio.dataCriacao).toLocaleString('pt-BR')}</td>
                    <td class="fw-bold text-primary">${audio.nomeColaborador}</td>
                    <td>${audio.cpfColaborador}</td>
                    <td><audio controls src="${audioUrl}" preload="none" class="w-100" style="height: 32px;"></audio></td>
                    <td>${status}</td>
                </tr>
            `;
        }
        tabelaHtml += '</tbody></table>';
        listaAudiosContainer.innerHTML = tabelaHtml;
    }

    // --- ** NOVAS FUNÇÕES DE EDIÇÃO ** ---

    function abrirModalEdicao(id, nome) {
        colaboradorEditandoId = id;
        editarModalTitulo.innerHTML = `<i class="bi bi-person-gear"></i> Gerenciando: ${nome}`;
        
        // Limpa formulários e status
        formEditarBancoHoras.reset();
        statusBancoHoras.textContent = '';
        formEditarFerias.reset();
        statusFerias.textContent = '';
        formEnviarHolerite.reset();
        statusHolerite.textContent = '';
        
        // Carrega histórico
        carregarHistoricoHolerites(id);

        bsModalEditar.show();
    }

    // Liga o formulário de Banco de Horas
    async function handleSalvarBancoHoras(e) {
        e.preventDefault();
        if (!adminToken || !colaboradorEditandoId) return;
        statusBancoHoras.textContent = 'Salvando...';
        statusBancoHoras.className = 'text-muted mt-2 small';

        const dados = {
            horasAcumuladas: parseFloat(document.getElementById('editar-horas').value),
            textoParaFala: document.getElementById('editar-horas-fala').value
        };

        try {
            const resposta = await fetch(`${API_BASE_URL}/api/admin/colaboradores/${colaboradorEditandoId}/bancohoras`, {
                method: 'POST',
                headers: {
                    'Authorization': `Bearer ${adminToken}`,
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(dados)
            });
            if (!resposta.ok) throw new Error('Falha ao salvar.');
            statusBancoHoras.textContent = 'Banco de Horas atualizado!';
            statusBancoHoras.className = 'text-success mt-2 fw-bold small';
        } catch (err) {
            statusBancoHoras.textContent = `Erro: ${err.message}`;
            statusBancoHoras.className = 'text-danger mt-2 fw-bold small';
        }
    }

    // Liga o formulário de Férias
    async function handleSalvarFerias(e) {
        e.preventDefault();
        if (!adminToken || !colaboradorEditandoId) return;
        statusFerias.textContent = 'Salvando...';
        statusFerias.className = 'text-muted mt-2 small';

        const dados = {
            dataInicio: document.getElementById('editar-ferias-inicio').value || null,
            dataFim: document.getElementById('editar-ferias-fim').value || null,
            diasDeSaldo: parseInt(document.getElementById('editar-ferias-saldo').value),
            textoParaFala: document.getElementById('editar-ferias-fala').value
        };

        try {
            const resposta = await fetch(`${API_BASE_URL}/api/admin/colaboradores/${colaboradorEditandoId}/ferias`, {
                method: 'POST',
                headers: {
                    'Authorization': `Bearer ${adminToken}`,
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(dados)
            });
            if (!resposta.ok) throw new Error('Falha ao salvar.');
            statusFerias.textContent = 'Dados de Férias atualizados!';
            statusFerias.className = 'text-success mt-2 fw-bold small';
        } catch (err) {
            statusFerias.textContent = `Erro: ${err.message}`;
            statusFerias.className = 'text-danger mt-2 fw-bold small';
        }
    }

    // Liga o formulário de Holerite
    async function handleEnviarHolerite(e) {
        e.preventDefault();
        if (!adminToken || !colaboradorEditandoId) return;
        statusHolerite.textContent = 'Enviando PDF...';
        statusHolerite.className = 'text-muted mt-2 small';

        const pdfFile = document.getElementById('holerite-pdf').files[0];
        if (!pdfFile) {
            statusHolerite.textContent = 'Por favor, selecione um arquivo PDF.';
            statusHolerite.className = 'text-warning mt-2 fw-bold small';
            return;
        }

        const formData = new FormData();
        formData.append('MesAno', document.getElementById('holerite-mes-ano').value);
        formData.append('ValorLiquido', parseFloat(document.getElementById('holerite-valor').value));
        formData.append('TextoParaFala', document.getElementById('holerite-fala').value);
        formData.append('pdfFile', pdfFile, pdfFile.name);

        try {
            const resposta = await fetch(`${API_BASE_URL}/api/admin/colaboradores/${colaboradorEditandoId}/holerite`, {
                method: 'POST',
                headers: { 'Authorization': `Bearer ${adminToken}` },
                body: formData
            });

            // CORREÇÃO DO BUG ORIGINAL: A verificação deve ser feita APÓS o fetch
            if (!resposta.ok) throw new Error('Falha ao enviar holerite.');
            
            statusHolerite.textContent = 'Holerite enviado com sucesso!';
            statusHolerite.className = 'text-success mt-2 fw-bold small';
            
            // Atualiza a lista logo após enviar
            carregarHistoricoHolerites(colaboradorEditandoId);
            
            // Opcional: limpar form
            formEnviarHolerite.reset();

        } catch (err) {
            statusHolerite.textContent = `Erro: ${err.message}`;
            statusHolerite.className = 'text-danger mt-2 fw-bold small';
        }
    }

    // --- Registro de Eventos ---
    loginFormAdmin.addEventListener('submit', fazerLoginAdmin);
    btnAdminSair.addEventListener('click', fazerLogoutAdmin);

    const btnFiltrar = document.getElementById('btn-filtrar');
    if (btnFiltrar) {
        btnFiltrar.addEventListener('click', () => {
            carregarColaboradores();
        });
    }

    // Botões do painel
    btnMostrarForm.addEventListener('click', () => {
        formColaboradorContainer.style.display = 'block';
        // Scroll suave até o formulário
        formColaboradorContainer.scrollIntoView({ behavior: 'smooth' });
    });
    
    btnCancelarForm.addEventListener('click', () => {
        formColaboradorContainer.style.display = 'none';
        formNovoColaborador.reset();
        formColaboradorStatus.textContent = '';
    });
    
    formNovoColaborador.addEventListener('submit', adicionarColaborador);

    // Navegação Sidebar
    navColaboradores.addEventListener('click', (e) => {
        e.preventDefault();
        mostrarPainelColaboradores();
    });
    navAudios.addEventListener('click', (e) => {
        e.preventDefault();
        mostrarPainelAudios();
    });

    // Modais
    formEditarBancoHoras.addEventListener('submit', handleSalvarBancoHoras);
    formEditarFerias.addEventListener('submit', handleSalvarFerias);
    formEnviarHolerite.addEventListener('submit', handleEnviarHolerite);

    // Inicialização
    const tokenSalvo = sessionStorage.getItem('admin_token');
    if (tokenSalvo) {
        adminToken = tokenSalvo;
        mostrarTela(telaDashboardAdmin);
    } else {
        mostrarTela(telaLoginAdmin);
    }

    // --- Lógica de Importação Geral ---
    const formImportacaoGeral = document.getElementById('form-importacao-geral');
    const statusImportacaoGeral = document.getElementById('status-importacao-geral');

    if (formImportacaoGeral) {
        formImportacaoGeral.addEventListener('submit', async (e) => {
            e.preventDefault();
            if (!adminToken) return;

            const arquivo = document.getElementById('arquivo-geral').files[0];
            if (!arquivo) return;

            statusImportacaoGeral.innerHTML = '<span class="text-warning"><i class="bi bi-hourglass-split"></i> Processando arquivo...</span>';

            const formData = new FormData();
            formData.append('arquivo', arquivo);

            try {
                const resposta = await fetch(`${API_BASE_URL}/api/admin/importar/geral`, {
                    method: 'POST',
                    headers: { 'Authorization': `Bearer ${adminToken}` },
                    body: formData
                });

                const dados = await resposta.json();

                if (resposta.ok) {
                    statusImportacaoGeral.innerHTML = `
                        <div class="alert alert-success mt-2 mb-0 py-2 small">
                            <strong><i class="bi bi-check-circle"></i> ${dados.message}</strong><br>
                            ${dados.detalhes || ''}
                        </div>`;
                } else {
                    throw new Error(dados.message);
                }
            } catch (err) {
                statusImportacaoGeral.innerHTML = `<div class="alert alert-danger mt-2 mb-0 py-2 small"><i class="bi bi-x-circle"></i> Erro: ${err.message}</div>`;
            }
        });
    }

    async function carregarHistoricoHolerites(id) {
        const tbody = document.getElementById('lista-historico-holerites');
        if (!tbody) return;

        tbody.innerHTML = '<tr><td colspan="3" class="text-center p-2"><div class="spinner-border spinner-border-sm text-secondary"></div></td></tr>';

        try {
            const resposta = await fetch(`${API_BASE_URL}/api/admin/colaboradores/${id}/holerites`, {
                headers: { 'Authorization': `Bearer ${adminToken}` }
            });
            const lista = await resposta.json();

            tbody.innerHTML = '';
            if (!lista || lista.length === 0) {
                tbody.innerHTML = '<tr><td colspan="3" class="text-center text-muted small">Nenhum histórico encontrado.</td></tr>';
                return;
            }

            lista.forEach(h => {
                const tr = document.createElement('tr');
                const valor = h.valorLiquido.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
                tr.innerHTML = `
                    <td>${h.mesAno}</td>
                    <td class="fw-bold text-success">${valor}</td>
                    <td><span class="d-inline-block text-truncate" style="max-width: 150px;" title="${h.textoParaFala}">${h.textoParaFala}</span></td>
                `;
                tbody.appendChild(tr);
            });
        } catch (err) {
            tbody.innerHTML = '<tr><td colspan="3" class="text-danger text-center small">Erro ao carregar histórico.</td></tr>';
        }
    }
});