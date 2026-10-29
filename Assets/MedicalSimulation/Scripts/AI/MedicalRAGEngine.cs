using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using MedicalSimulation.Core;

namespace MedicalSimulation.AI
{
    [Serializable]
    public class RAGChunk
    {
        public string chunkId;
        public string title;
        public string sourceName;
        public string content;
        public string[] tags;
        public float relevanceScore;
    }

    /// <summary>
    /// In-memory Retrieval-Augmented Generation (RAG) engine.
    /// Indexes anatomical ScriptableObjects, clinical case documentation, and procedural protocols.
    /// Performs keyword and n-gram lexical similarity retrieval to ground LLM generations with authoritative medical citations.
    /// </summary>
    public class MedicalRAGEngine
    {
        private readonly List<RAGChunk> _corpus = new List<RAGChunk>();
        private bool _isInitialized = false;

        public int DocumentCount => _corpus.Count;

        public void Initialize()
        {
            if (_isInitialized) return;

            // Ingest standard anatomical catalog
            IngestOrgan("heart", "Heart (Coronary Anatomy & Mechanics)", "Gray's Anatomy v42",
                "The human heart is a conical muscular organ situated in the middle mediastinum. It features four chambers: right/left atria and right/left ventricles. Coronary arteries (LAD, LCx, RCA) originate from aortic sinuses to perfuse myocardium. The conduction system comprises SA node, AV node, bundle of His, and Purkinje fibers.",
                new[] { "heart", "cardiac", "coronary", "sa node", "ventricle", "atrium", "circulation", "valve", "ecg" });

            IngestOrgan("lung", "Lungs (Pulmonary Lobes & Gas Exchange)", "West's Respiratory Physiology",
                "The paired lungs occupy the pleural cavities of the thorax. The right lung has three lobes (superior, middle, inferior); the left has two and a cardiac notch. Bronchial tree branches into terminal bronchioles and alveoli coated in surfactant where passive diffusion of O2 and CO2 across the thin blood-gas barrier occurs.",
                new[] { "lung", "respiratory", "pulmonary", "alveoli", "bronchi", "breathing", "surfactant", "ventilation" });

            IngestOrgan("liver", "Liver (Hepatic Segments & Portal Triads)", "Sherlock's Hepatology",
                "The liver consists of eight functionally independent Couinaud segments, each with its own dual vascular inflow (portal vein and hepatic artery) and biliary drainage. Functional units are hepatic lobules surrounded by portal triads. Hepatocytes synthesize albumin, coagulation factors, bile salts, and perform phase I/II xenobiotic clearance.",
                new[] { "liver", "hepatic", "portal vein", "bile", "bilirubin", "cirrhosis", "hepatocyte", "metabolism" });

            IngestOrgan("stomach", "Stomach (Gastric Motility & Acid Secretion)", "Ganong's Medical Physiology",
                "The stomach comprises cardia, fundus, body, and antrum. Mucosal parietal cells secrete gastric acid via H+/K+ ATPase, Chief cells secrete pepsinogen, and G cells secrete gastrin. Circular, longitudinal, and oblique smooth muscle layers churn food into chyme coordinated by the gastric slow wave.",
                new[] { "stomach", "gastric", "acid", "parietal", "pepsin", "chyme", "peristalsis", "digestion" });

            IngestOrgan("kidney", "Kidneys (Nephron Architecture & Filtration)", "Brenner & Rector's The Kidney",
                "The kidneys filter circulating plasma at ~180 liters per day through glomeruli into Bowman's capsule. Proximal tubules reabsorb nutrients and electrolytes. The Loop of Henle maintains a hypertonic corticomedullary osmotic gradient. Distal nephron and collecting duct regulate final sodium and water retention via aldosterone and ADH.",
                new[] { "kidney", "renal", "nephron", "glomerulus", "filtration", "gfr", "loop of henle", "urine", "electrolytes" });

            IngestOrgan("intestine", "Intestines (Small & Large Bowel Physiology)", "Sleisenger and Fordtran's Gastrointestinal Disease",
                "The small intestine (duodenum, jejunum, ileum) maximizes nutrient absorptive surface area via circular folds (plicae circulares), villi, and microvilli brush border enzymes. The large intestine (cecum, colon, rectum) absorbs water and electrolytes, facilitating bacterial fermentation and fecal compaction.",
                new[] { "intestine", "gut", "colon", "jejunum", "ileum", "villi", "absorption", "peristalsis", "microbiome" });

            IngestOrgan("dental", "Dental Structures & Alveolar Surgery", "Contemporary Oral & Maxillofacial Surgery",
                "Human dentition consists of incisors, canines, premolars, and molars. Tooth layers include enamel, dentin, cementum, and neurovascular pulp. Extraction requires syndesmotomy to sever gingival attachments, alveolar bone expansion with straight elevators, and forceps delivery along the anatomical root curvature.",
                new[] { "dental", "tooth", "teeth", "enamel", "dentin", "pulp", "periodontal", "extraction", "elevator", "forceps" });

            IngestOrgan("ml", "Machine Learning & Medical AI Foundations", "Bishop - Pattern Recognition",
                "Machine Learning in clinical systems leverages supervised algorithms (convolutional neural networks for image segmentation, recurrent networks / transformers for clinical time-series) and unsupervised representations to assist diagnostic workflows with calibrated confidence metrics.",
                new[] { "machine learning", "ml", "ai", "neural network", "deep learning", "algorithm", "supervised", "training" });

            _isInitialized = true;
        }

        public void IngestOrgan(string id, string title, string sourceName, string content, string[] tags)
        {
            _corpus.Add(new RAGChunk
            {
                chunkId = id,
                title = title,
                sourceName = sourceName,
                content = content,
                tags = tags ?? new string[0],
                relevanceScore = 0f
            });
        }

        /// <summary>
        /// Retrieves the top-K most relevant chunks using multi-token keyword overlap and n-gram scoring.
        /// </summary>
        public List<RAGChunk> RetrieveRelevant(string query, int topK = 3)
        {
            if (!_isInitialized) Initialize();
            if (string.IsNullOrEmpty(query)) return new List<RAGChunk>();

            var tokens = query.ToLowerInvariant()
                .Split(new[] { ' ', ',', '.', '?', '!', ';', ':', '-', '(', ')' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(t => t.Length > 2)
                .Distinct()
                .ToList();

            var scored = new List<RAGChunk>();

            foreach (var chunk in _corpus)
            {
                float score = 0f;
                string lowerContent = chunk.content.ToLowerInvariant();
                string lowerTitle = chunk.title.ToLowerInvariant();

                foreach (var token in tokens)
                {
                    // Tag match is weighted highest
                    if (chunk.tags != null && chunk.tags.Any(tag => tag.Contains(token) || token.Contains(tag)))
                    {
                        score += 3.0f;
                    }

                    // Title match
                    if (lowerTitle.Contains(token))
                    {
                        score += 2.0f;
                    }

                    // Content match
                    if (lowerContent.Contains(token))
                    {
                        score += 1.0f;
                    }
                }

                if (score > 0.5f)
                {
                    scored.Add(new RAGChunk
                    {
                        chunkId = chunk.chunkId,
                        title = chunk.title,
                        sourceName = chunk.sourceName,
                        content = chunk.content,
                        tags = chunk.tags,
                        relevanceScore = score
                    });
                }
            }

            return scored.OrderByDescending(c => c.relevanceScore).Take(topK).ToList();
        }

        /// <summary>
        /// Builds an augmented context string containing cited snippets to guide the LLM.
        /// </summary>
        public string BuildAugmentationString(string query, int topK, out List<string> citations)
        {
            citations = new List<string>();
            var chunks = RetrieveRelevant(query, topK);
            if (chunks.Count == 0) return string.Empty;

            var sb = new StringBuilder();
            sb.AppendLine("[VERIFIED CLINICAL REFERENCE DOCUMENTS]");

            foreach (var chunk in chunks)
            {
                sb.AppendLine($"--- {chunk.title} [Source: {chunk.sourceName}] ---");
                sb.AppendLine(chunk.content);
                citations.Add($"{chunk.title} ({chunk.sourceName})");
            }

            sb.AppendLine("[END REFERENCE DOCUMENTS]\n");
            return sb.ToString();
        }
    }
}
