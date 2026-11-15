using System;
using System.Collections.Generic;
using NybuOS.Core.FontEngine;
using NybuOS.Core.Graphics;
using NybuOS.Core.Input;

namespace NybuOS.Apps;

public class HelpTopic
{
    public string Title { get; set; } = "";
    public string Category { get; set; } = "";
    public string[] ContentLines { get; set; } = Array.Empty<string>();
}

public class GetHelpApp : AppBase
{
    public override string Id => "help";
    public override string Title => "Obter Ajuda";
    public override FastBitmap? Icon16 => AssetManager.IconHelp16;
    public override FastBitmap? Icon24 => AssetManager.IconHelp24;
    public override FastBitmap? Icon48 => AssetManager.IconHelp48;

    private readonly List<HelpTopic> _allTopics = new();
    private readonly List<HelpTopic> _filteredTopics = new();
    private int _selectedTopicIndex = 0;
    private string _searchQuery = "";
    private bool _searchActive = false;

    public override void Initialize(Core.WindowManager.Window window)
    {
        base.Initialize(window);
        BuildDatabase();
        FilterTopics();
    }

    private void BuildDatabase()
    {
        _allTopics.Clear();

        _allTopics.Add(new HelpTopic
        {
            Title = "Visão Geral da Interface",
            Category = "Sistema",
            ContentLines = new[]
            {
                "O NybuOS V Lagoons possui uma interface clara inspirada no Windows 10.",
                "- Barra de Tarefas: Fundo branco sólido (#FFFFFF) com linha divisória sutil.",
                "- Botão Iniciar: Canto esquerdo sem texto, apenas com o ícone oficial do NybuOS.",
                "- Relógio Digital: Canto direito exibindo hora e minutos em tempo real.",
                "- Janelas: Design quadrado, bordas finas, sombra suave e fundo branco (#FFFFFF).",
                "- Maximizar: A janela ocupa toda a área útil recalculando componentes dinamicamente."
            }
        });

        _allTopics.Add(new HelpTopic
        {
            Title = "Menu Iniciar (3 Colunas)",
            Category = "Sistema",
            ContentLines = new[]
            {
                "A arquitetura do Menu Iniciar é estruturada estritamente em três colunas:",
                "1. Coluna Esquerda: Ações rápidas (Menu sanduíche, Usuário, Documentos, Configurações, Desligar).",
                "2. Coluna Central: Lista alfabética vertical rolável de todos os 12 aplicativos instalados.",
                "3. Coluna Direita: Grade de Blocos Dinâmicos (Live Tiles) de tamanhos pequeno, médio e largo."
            }
        });

        _allTopics.Add(new HelpTopic
        {
            Title = "Sistema de Arquivos FAT32 Real",
            Category = "Subsistemas",
            ContentLines = new[]
            {
                "O NybuOS inicializa o CosmosVFS e monta o volume FAT32 real em 0:\\.",
                "Todos os aplicativos persistem dados diretamente em disco:",
                "- 0:\\documentos : Arquivos de texto (.txt) criados no Bloco de Notas.",
                "- 0:\\planilhas  : Planilhas e tabelas estruturadas (.csv).",
                "- 0:\\desenhos   : Imagens criadas no PaintPad (.bmp).",
                "- 0:\\sistema   : Configurações, eventos e registros de sistema."
            }
        });

        _allTopics.Add(new HelpTopic
        {
            Title = "Gerenciador de Arquivos",
            Category = "Aplicativos",
            ContentLines = new[]
            {
                "Navegador completo de arquivos para o disco FAT32 (0:\\).",
                "- Navegação por pastas com botão 'Voltar'.",
                "- Criação de novas pastas com o botão '+ Nova Pasta'.",
                "- Exclusão de arquivos e diretórios selecionados.",
                "- Clique duplo em arquivos de texto (.txt) abre-os no Bloco de Notas."
            }
        });

        _allTopics.Add(new HelpTopic
        {
            Title = "Calculadora",
            Category = "Aplicativos",
            ContentLines = new[]
            {
                "Calculadora padrão de alta precisão:",
                "- Suporte às quatro operações básicas (+, -, ×, ÷).",
                "- Suporte a decimais e inversão de sinal (±).",
                "- Histórico de cálculos recentes na parte superior.",
                "- Limpeza rápida com os botões CE (entrada) e C (total)."
            }
        });

        _allTopics.Add(new HelpTopic
        {
            Title = "CentralLogic (Campo Minado)",
            Category = "Jogos",
            ContentLines = new[]
            {
                "Clone fiel do jogo clássico Campo Minado:",
                "- Botão esquerdo do mouse revela o quadrado.",
                "- Se um quadrado sem minas ao redor for aberto, ocorre cascateamento automático.",
                "- Botão direito do mouse adiciona ou remove bandeiras (🚩).",
                "- Botão de rosto reinicia a partida a qualquer momento."
            }
        });

        _allTopics.Add(new HelpTopic
        {
            Title = "Calendário",
            Category = "Aplicativos",
            ContentLines = new[]
            {
                "Visualização em grade mensal completa:",
                "- Navegação entre meses com as setas ◀ e ▶.",
                "- Clique em qualquer dia para selecioná-lo e ver/adicionar lembretes.",
                "- Dias com eventos salvos exibem um indicador circular colorido.",
                "- Todos os eventos são persistidos no arquivo 0:\\sistema\\eventos.txt."
            }
        });

        _allTopics.Add(new HelpTopic
        {
            Title = "Terminal NybuOS",
            Category = "Desenvolvedores",
            ContentLines = new[]
            {
                "Interpretador de comandos (CLI) completo integrado ao sistema:",
                "- dir / ls : lista arquivos e pastas do diretório atual.",
                "- cd <caminho> : navega entre pastas.",
                "- mkdir <pasta> : cria uma nova pasta.",
                "- cat <arquivo> : visualiza arquivos de texto.",
                "- rm <caminho> : remove arquivo ou pasta.",
                "- sysinfo : informações técnicas do kernel Cosmos Gen 3.",
                "- theme <cor> : altera a cor de destaque em tempo real.",
                "- kill <appId> : fecha janelas e processos de aplicativos."
            }
        });

        _allTopics.Add(new HelpTopic
        {
            Title = "Configurações",
            Category = "Sistema",
            ContentLines = new[]
            {
                "Central de personalização do sistema:",
                "- Escolha entre 6 cores de destaque: Azul, Laranja, Roxo, Verde, Amarelo e Vermelho.",
                "- O sistema atualiza botões, bordas e detalhes imediatamente sem reiniciar.",
                "- Controle deslizante para ajustar o brilho geral da tela.",
                "- Controle deslizante para ajuste de volume do sistema de áudio."
            }
        });

        _allTopics.Add(new HelpTopic
        {
            Title = "Bloco de Notas",
            Category = "Produtividade",
            ContentLines = new[]
            {
                "Editor de texto completo para manipulação de arquivos .txt:",
                "- Cursor interativo com suporte a digitação por teclado e navegação por setas.",
                "- Menu de Arquivo com opções Novo, Salvar e Abrir Padrão.",
                "- Barra de status com número da linha, coluna e total de caracteres.",
                "- Grava e lê texto puro diretamente no sistema FAT32."
            }
        });

        _allTopics.Add(new HelpTopic
        {
            Title = "PaintPad",
            Category = "Criatividade",
            ContentLines = new[]
            {
                "Micro editor gráfico estilo MS Paint:",
                "- Área de desenho (canvas) com pintura fluida e sem atrasos.",
                "- Ferramentas: Pincel, Borracha, Linha reta e Retângulo.",
                "- Paleta com 12 cores vibrantes para seleção instantânea.",
                "- Botão 'Salvar' grava o desenho no formato Bitmap 24-bit (0:\\desenhos\\desenho.bmp)."
            }
        });

        _allTopics.Add(new HelpTopic
        {
            Title = "Saúde do Sistema",
            Category = "Segurança",
            ContentLines = new[]
            {
                "Ferramenta de diagnóstico e verificação de integridade:",
                "- Analisa todos os arquivos e partições do disco FAT32.",
                "- Detecta anomalias de arquivos nulos, extensões suspeitas ou setores corrompidos.",
                "- Exibe relatório em tempo real com barra de progresso animada.",
                "- Botão de reparo corrige automaticamente incongruências encontradas."
            }
        });

        _allTopics.Add(new HelpTopic
        {
            Title = "Planilha NybuOS",
            Category = "Produtividade",
            ContentLines = new[]
            {
                "Editor de planilhas funcional baseado em grade de células:",
                "- Colunas de A até Z e linhas de 1 até 50.",
                "- Barra de fórmulas superior para edição e visualização de valores.",
                "- Fórmulas suportadas: =SOMA(A1:A5), =MEDIA(A1:A5) e operações aritméticas =A1+B1.",
                "- Suporte para salvar e carregar dados estruturados no FAT32."
            }
        });
    }

    private void FilterTopics()
    {
        _filteredTopics.Clear();
        string q = _searchQuery.Trim().ToLowerInvariant();

        for (int i = 0; i < _allTopics.Count; i++)
        {
            var t = _allTopics[i];
            if (string.IsNullOrEmpty(q) || t.Title.ToLowerInvariant().Contains(q) || t.Category.ToLowerInvariant().Contains(q))
            {
                _filteredTopics.Add(t);
            }
        }

        if (_selectedTopicIndex >= _filteredTopics.Count)
        {
            _selectedTopicIndex = Math.Max(0, _filteredTopics.Count - 1);
        }
    }

    public override void Render(uint[] buffer, int screenW, int screenH, int clientX, int clientY, int clientW, int clientH)
    {
        FastRenderer.DrawFilledRectangle(buffer, screenW, screenH, clientX, clientY, clientW, clientH, ColorScheme.WindowBody);

        // 1. Search Bar at Top (46px)
        int searchH = 46;
        FastRenderer.DrawFilledRectangle(buffer, screenW, screenH, clientX, clientY, clientW, searchH, ColorScheme.BackgroundLight);
        FastRenderer.DrawHorizontalLine(buffer, screenW, screenH, clientX, clientY + searchH, clientW, ColorScheme.WindowBorder);

        int boxX = clientX + 16;
        int boxY = clientY + 8;
        int boxW = clientW - 32;
        int boxH = 30;

        FastRenderer.DrawFilledRectangle(buffer, screenW, screenH, boxX, boxY, boxW, boxH, ColorScheme.WindowBody);
        FastRenderer.DrawRectangle(buffer, screenW, screenH, boxX, boxY, boxW, boxH, _searchActive ? ColorScheme.Accent : ColorScheme.WindowBorder);

        FontRenderer.DrawText(buffer, screenW, screenH, "🔍", boxX + 8, boxY + 6, ColorScheme.TextSecondary, bold: false, size: 14);

        string searchShow = string.IsNullOrEmpty(_searchQuery) ? (_searchActive ? "" : "Pesquisar tópicos de ajuda...") : _searchQuery;
        uint searchCol = string.IsNullOrEmpty(_searchQuery) ? ColorScheme.TextMuted : ColorScheme.TextPrimary;
        FontRenderer.DrawText(buffer, screenW, screenH, searchShow, boxX + 32, boxY + 7, searchCol, bold: false, size: 12);

        // 2. Split View: Left List (220px) & Right Content
        int listW = 220;
        int contentY = clientY + searchH;
        int contentH = clientH - searchH;

        FastRenderer.DrawFilledRectangle(buffer, screenW, screenH, clientX, contentY, listW, contentH, ColorScheme.BackgroundLight);
        FastRenderer.DrawVerticalLine(buffer, screenW, screenH, clientX + listW, contentY, contentH, ColorScheme.WindowBorder);

        // Render Topics List
        int rowY = contentY + 10;
        for (int i = 0; i < _filteredTopics.Count; i++)
        {
            if (rowY + 34 > clientY + clientH) break;

            bool isSel = (i == _selectedTopicIndex);
            if (isSel)
            {
                FastRenderer.DrawFilledRectangle(buffer, screenW, screenH, clientX + 4, rowY, listW - 8, 30, ColorScheme.ControlBackground);
                FastRenderer.DrawFilledRectangle(buffer, screenW, screenH, clientX + 4, rowY, 4, 30, ColorScheme.Accent);
            }

            var topic = _filteredTopics[i];
            FontRenderer.DrawText(buffer, screenW, screenH, topic.Title, clientX + 14, rowY + 4, isSel ? ColorScheme.Accent : ColorScheme.TextPrimary, bold: isSel, size: 12);
            FontRenderer.DrawText(buffer, screenW, screenH, topic.Category, clientX + 14, rowY + 18, ColorScheme.TextMuted, bold: false, size: 12);

            rowY += 34;
        }

        // Render Right Content
        if (_filteredTopics.Count > 0 && _selectedTopicIndex < _filteredTopics.Count)
        {
            var sel = _filteredTopics[_selectedTopicIndex];
            int rightX = clientX + listW + 24;
            int rightY = contentY + 16;

            // Header (Bold Segoe UI)
            FontRenderer.DrawText(buffer, screenW, screenH, sel.Title, rightX, rightY, ColorScheme.TextPrimary, bold: true, size: 18);
            FontRenderer.DrawText(buffer, screenW, screenH, $"Categoria: {sel.Category}", rightX, rightY + 26, ColorScheme.Accent, bold: false, size: 12);

            FastRenderer.DrawHorizontalLine(buffer, screenW, screenH, rightX, rightY + 44, clientW - listW - 48, ColorScheme.WindowBorder);

            // Article Body Text
            int textY = rightY + 54;
            for (int i = 0; i < sel.ContentLines.Length; i++)
            {
                if (textY + 20 > clientY + clientH - 10) break;
                string line = sel.ContentLines[i];
                bool isBullet = line.StartsWith("-");
                FontRenderer.DrawText(buffer, screenW, screenH, line, rightX + (isBullet ? 8 : 0), textY, ColorScheme.TextPrimary, bold: false, size: 12);
                textY += 22;
            }
        }
    }

    public override void HandleInput(int clientX, int clientY, int clientW, int clientH)
    {
        int mx = InputState.MouseX;
        int my = InputState.MouseY;

        if (InputState.LeftClicked)
        {
            // Search box click
            int boxX = clientX + 16;
            int boxY = clientY + 8;
            int boxW = clientW - 32;
            int boxH = 30;

            if (mx >= boxX && mx < boxX + boxW && my >= boxY && my < boxY + boxH)
            {
                _searchActive = true;
                return;
            }
            _searchActive = false;

            // Topics list click
            int listW = 220;
            int contentY = clientY + 46;
            if (mx >= clientX && mx < clientX + listW && my >= contentY)
            {
                int rowY = contentY + 10;
                for (int i = 0; i < _filteredTopics.Count; i++)
                {
                    if (my >= rowY && my < rowY + 34)
                    {
                        _selectedTopicIndex = i;
                        return;
                    }
                    rowY += 34;
                }
            }
        }

        // Search text input
        if (_searchActive)
        {
            while (InputState.TryReadKey(out var keyEvent))
            {
                if (keyEvent.Key == Cosmos.Kernel.System.Input.Key.Backspace)
                {
                    if (_searchQuery.Length > 0)
                    {
                        _searchQuery = _searchQuery.Substring(0, _searchQuery.Length - 1);
                        FilterTopics();
                    }
                }
                else if (keyEvent.KeyChar >= ' ' && keyEvent.KeyChar <= '~')
                {
                    if (_searchQuery.Length < 30)
                    {
                        _searchQuery += keyEvent.KeyChar;
                        FilterTopics();
                    }
                }
            }
        }
    }
}
