using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Onudhabon_ISD.Data;
using Onudhabon_ISD.Models;

namespace Onudhabon_ISD.Data
{
    public static class ClassPlanHelper
    {
        // Standard NCTB Curriculum Topics catalog mapped by subject
        private static readonly Dictionary<string, List<string>> _subjectTopicsMap = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Bangla"] = new()
            {
                "বর্ণমালা ও স্বরচিহ্ন (Alphabet & Vowels)",
                "যুক্তবর্ণ ও শব্দ গঠন (Word Formation)",
                "বাক্য গঠন (Sentence Construction)",
                "ছড়া ও কবিতা পাঠ (Rhymes & Poems)",
                "গল্প ও অনুচ্ছেদ পাঠ (Short Stories & Reading)",
                "হাতের লেখা ও বানান সতর্কতা (Handwriting & Spelling)",
                "বিপরীত শব্দ ও সমার্থক শব্দ (Antonyms & Synonyms)",
                "সহজ ব্যাকরণ ও পদ পরিচয় (Basic Grammar)"
            },

            ["Bangla 1st paper"] = new()
            {
                "গদ্য ও প্রবন্ধ পাঠ (Prose & Essays)",
                "কবিতা ও কাব্য বিশ্লেষণ (Poetry Analysis)",
                "উপন্যাস পাঠ ও চরিত্র বিশ্লেষণ (Novel Study)",
                "নাটক ও নাট্যসাহিত্য (Drama & Play Study)",
                "সৃজনশীল প্রশ্ন ও উত্তর কৌশল (Creative Questions & Answers)",
                "শব্দার্থ, টীকা ও মূলভাব (Word Meanings & Central Idea)",
                "লেখক ও কবি পরিচিতি (Author & Poet Biographies)",
                "অনুশীলনী ও বহুনিবার্চনী প্রশ্নোত্তর (MCQ & Short Questions)"
            },

            ["Bangla 2nd paper"] = new()
            {
                "ধ্বনিতত্ত্ব ও বর্ণ প্রকরণ (Phonetics & Letters)",
                "সন্ধি (Sandhi)",
                "শব্দ ও পদ প্রকরণ (Parts of Speech)",
                "কারক ও বিভক্তি (Case & Inflexion)",
                "সমাস (Samas)",
                "উপসর্গ ও প্রত্যয় (Prefixes & Suffixes)",
                "ণ-ত্ব ও ষ-ত্ব বিধান (Natwa & Shatwa Vidhan)",
                "বাক্য প্রকরণ ও রূপান্তর (Sentence Types & Transformation)",
                "বিরামচিহ্ন ও যতিচিহ্ন (Punctuation Marks)",
                "বাগধারা ও প্রবাদ-প্রবচন (Idioms & Proverbs)",
                "ভাবসম্প্রসারণ (Amplification of Ideas)",
                "অনুচ্ছেদ ও রচনা লিখন (Paragraph & Essay Writing)",
                "চিঠি, আবেদনপত্র ও ইমেইল (Letters, Applications & Emails)",
                "সারাংশ ও সারমর্ম লিখন (Summary & Précis Writing)",
                "প্রতিবেদন প্রণয়ন (Report Writing)"
            },

            ["English"] = new()
            {
                "Alphabet & Phonics",
                "Basic Vocabulary & Word Building",
                "Greetings & Simple Conversations",
                "Numbers, Colors & Shapes",
                "Action Words & Simple Verbs",
                "Rhymes & Short Stories",
                "Simple Sentence Construction",
                "Picture Description & Comprehension"
            },

            ["English 1st paper"] = new()
            {
                "Seen Reading Comprehension",
                "Unseen Reading Comprehension",
                "Information Transfer & True/False",
                "Flow Chart & Summary Writing",
                "Theme Writing & Appreciation",
                "Cloze Test with Clues",
                "Cloze Test without Clues",
                "Rearranging Sentences",
                "Paragraph Writing",
                "Story Writing & Completing Stories",
                "Informal Letters & Emails",
                "Formal Letters & Applications",
                "Dialogue Writing",
                "Graph & Chart Description"
            },

            ["English 2nd paper"] = new()
            {
                "Parts of Speech & Identification",
                "Articles & Determiners",
                "Right Form of Verbs",
                "Tense & Subject-Verb Agreement",
                "Voice Change (Active & Passive)",
                "Narration (Direct & Indirect Speech)",
                "Transformation of Sentences (Simple, Complex, Compound)",
                "Completing Sentences & Conditionals",
                "Modifiers (Pre & Post Modifiers)",
                "Sentence Connectors & Linkers",
                "Synonyms & Antonyms",
                "Tag Questions",
                "Punctuation & Capitalization",
                "Prefixes & Suffixes",
                "CV Writing with Cover Letter",
                "Composition & Essay Writing"
            },

            ["Math"] = new()
            {
                "যোগ, বিয়োগ, গুণ ও ভাগ (Basic Arithmetic & Four Operations)",
                "গ.সা.গু ও ল.সা.গু (HCF & LCM)",
                "ভগ্নাংশ ও দশমিক (Fractions & Decimals)",
                "ঐকিক নিয়ম ও অনুপাত (Unitary Method & Ratio)",
                "শতকরা, লাভ-ক্ষতি ও সরল মুনাফা (Percentage, Profit-Loss & Interest)",
                "বীজগণিতীয় রাশি ও সূত্রাবলি (Algebraic Expressions & Formulas)",
                "উৎপাদকে বিশ্লেষণ (Factorization)",
                "সরল ও দ্বিঘাত সমীকরণ (Linear & Quadratic Equations)",
                "সেট ও ফাংশন (Set & Function)",
                "বাস্তব সংখ্যা ও সূচক-লগারিদম (Real Numbers, Exponents & Logarithms)",
                "সমান্তর ও গুণোত্তর ধারা (Arithmetic & Geometric Series)",
                "জ্যামিতি: কোণ, ত্রিভুজ ও চতুর্ভুজ (Geometry: Angles, Triangles & Quadrilaterals)",
                "বৃত্ত ও উপপাদ্য (Circles & Theorems)",
                "ব্যবহারিক জ্যামিতি ও সম্পাদ্য (Practical Geometry)",
                "ত্রিকোণমিতিক অনুপাত ও অভেদ (Trigonometric Ratios & Identities)",
                "দূরত্ব ও উচ্চতা (Distance & Elevation)",
                "পরিমিতি ও ক্ষেত্রফল (Mensuration & Area)",
                "পরিসংখ্যান ও তথ্য-উপাত্ত (Statistics & Data Handling)"
            },

            ["Higher Math"] = new()
            {
                "সেট ও ফাংশন (Set & Function)",
                "বীজগাণিতিক রাশি (Algebraic Expressions)",
                "জ্যামিতি ও অ্যাপোলোনিয়াসের উপপাদ্য (Geometry & Apollonius Theorem)",
                "স্থানাঙ্ক জ্যামিতি (Coordinate Geometry)",
                "দ্বিপদী বিস্তৃতি (Binomial Expansion)",
                "সমীকরণ ও অসমতা (Equations & Inequalities)",
                "ভেক্টর (Vectors)",
                "ত্রিকোণমিতি ও রেডিয়ান কোণ (Trigonometry & Radian Measure)",
                "সম্ভাবনা (Probability)",
                "ঘন জ্যামিতি (Solid Geometry)"
            },

            ["Higher Math 1st paper"] = new()
            {
                "ম্যাট্রিক্স ও নির্ণায়ক (Matrices & Determinants)",
                "ভেক্টর (Vectors)",
                "সরলরেখা (Straight Lines)",
                "বৃত্ত (Circles)",
                "ত্রিকোণমিতিক অনুপাত (Trigonometric Ratios & Transformations)",
                "সংযুক্ত কোণের ত্রিকোণমিতিক অনুপাত (Compound Angles)",
                "ফাংশন ও ফাংশনের লেখচিত্র (Functions & Graphs)",
                "অন্তরীকরণ ও লিমিট (Differentiation & Limits)",
                "অন্তরীকরণের প্রয়োগ (Applications of Derivatives)",
                "যোগজীকরণ (Integration & Definite Integrals)"
            },

            ["Higher Math 2nd paper"] = new()
            {
                "বাস্তব সংখ্যা ও অসমতা (Real Numbers & Inequalities)",
                "জটিল সংখ্যা (Complex Numbers)",
                "বহুপদী ও বহুপদী সমীকরণ (Polynomials & Quadratic Equations)",
                "কণিক: পরাবৃত্ত, উপবৃত্ত ও অধিবৃত্ত (Conics: Parabola, Ellipse, Hyperbola)",
                "বিপরীত ত্রিকোণমিতিক ফাংশন ও সমীকরণ (Inverse Trigonometric Functions)",
                "স্থিতিবিদ্যা ও বলের সাম্যাবস্থা (Statics & Equilibrium of Forces)",
                "সমতলে বস্তুকণার গতি (Dynamics & Particle Motion)",
                "সম্ভাবনা ও সম্ভাবনা বিন্যাস (Probability & Distributions)"
            },

            ["General Science"] = new()
            {
                "জীবজগৎ ও শ্রেণিবিন্যাস (Living World & Classification)",
                "উদ্ভিদ ও প্রাণীর কোষীয় সংগঠন (Cellular Organization)",
                "উদ্ভিদের বাহ্যিক বৈশিষ্ট্য ও রূপান্তর (Plant Morphology)",
                "পরিপাকতন্ত্র এবং রক্ত সংবহনতন্ত্র (Digestive & Circulatory Systems)",
                "পদার্থের গঠন ও পরমাণুর ধারণা (Structure of Matter & Atoms)",
                "অম্ল, ক্ষারক ও লবণ (Acids, Bases & Salts)",
                "তাপ ও তাপমাত্রা (Heat & Temperature)",
                "আলো ও দৃষ্টির ক্রিয়া (Light & Optics)",
                "শব্দ ও এর প্রকৃতি (Sound & Acoustics)",
                "গতি ও বল (Motion & Force)",
                "বিদ্যুৎ ও চৌম্বক ক্রিয়া (Electricity & Magnetism)",
                "পরিবেশ ও বাস্তুসংস্থান (Environment & Ecosystem)",
                "জলবায়ু পরিবর্তন ও দুর্যোগ মোকাবেলা (Climate Change & Disasters)"
            },

            ["Social Science"] = new()
            {
                "বাংলাদেশের ইতিহাস ও প্রাচীন সভ্যতা (History of Bangladesh & Ancient Civilizations)",
                "ভাষা আন্দোলন ও মুক্তিযুদ্ধ (Language Movement & Liberation War of 1971)",
                "ভূপ্রকৃতি, নদ-নদী ও জলবায়ু (Geography, Rivers & Climate)",
                "বাংলাদেশের প্রাকৃতিক সম্পদ ও কৃষি (Natural Resources & Agriculture)",
                "শিল্প, বাণিজ্য ও অর্থনৈতিক উন্নয়ন (Industry, Trade & Economic Growth)",
                "নাগরিক অধিকার, কর্তব্য ও মূল্যবোধ (Civic Rights, Duties & Values)",
                "সংবিধান, সরকার ও রাষ্ট্র পরিচালনা (Constitution & Government Structure)",
                "সামাজিক সমস্যা ও প্রতিকার (Social Issues, Crime & Solutions)",
                "পরিবার ও সামাজিকীকরণ (Family & Socialization)",
                "আন্তর্জাতিক সহযোগিতা ও জাতিসংঘ (UN & Global Partnerships)"
            },

            ["Physics"] = new()
            {
                "ভৌত রাশি ও পরিমাপ (Physical Quantities & Measurement)",
                "গতি ও গতিবিজ্ঞান (Motion & Kinematics)",
                "বল ও নিউটনের গতিসূত্র (Force & Newton's Laws)",
                "কাজ, ক্ষমতা ও শক্তি (Work, Power & Energy)",
                "পদার্থের অবস্থা ও চাপ (States of Matter, Density & Pressure)",
                "বস্তুর ওপর তাপের প্রভাব (Effects of Heat on Matter)",
                "তরঙ্গ ও শব্দ (Waves & Sound)",
                "আলোর প্রতিফলন ও দর্পণ (Reflection of Light & Mirrors)",
                "আলোর প্রতিসরণ ও লেন্স (Refraction of Light & Lenses)",
                "স্থির বিদ্যুৎ ও কুলম্বের সূত্র (Static Electricity & Coulomb's Law)",
                "চল বিদ্যুৎ, ওহমের সূত্র ও বর্তনী (Current Electricity & Circuits)",
                "বিদ্যুতের চৌম্বক ক্রিয়া (Magnetic Effects of Current)",
                "আধুনিক পদার্থবিজ্ঞান ও ইলেকট্রনিক্স (Modern Physics & Electronics)",
                "জীবন রক্ষায় পদার্থবিজ্ঞান (Physics in Healthcare & Technology)"
            },

            ["Physics 1st paper"] = new()
            {
                "ভৌত জগৎ ও পরিমাপ (Physical World & Measurement)",
                "ভেক্টর ও স্কেলার রাশি (Vectors & Calculus in Physics)",
                "গতিবিদ্যা ও প্রক্ষেপকের গতি (Kinematics & Projectile Motion)",
                "নিউটনিয়ান বলবিদ্যা ও রৈখিক ভরবেগ (Newtonian Mechanics & Momentum)",
                "কাজ, শক্তি ও ক্ষমতা (Work, Energy & Power)",
                "মহাকর্ষ ও অভিকর্ষ (Gravitation & Gravitational Potential)",
                "পদার্থের গাঠনিক ধর্ম ও স্থিতিস্থাপকতা (Properties of Matter & Elasticity)",
                "পৃষ্ঠটান, সান্দ্রতা ও প্রবাহী (Surface Tension & Viscosity)",
                "পর্যায়বৃত্ত গতি ও সরল ছন্দিত স্পন্দন (Periodic Motion & SHM)",
                "তরঙ্গ, শব্দের বেগ ও ডপলার ক্রিয়া (Waves, Sound & Doppler Effect)",
                "আদর্শ গ্যাস ও গ্যাসের গতিতত্ত্ব (Ideal Gas & Kinetic Theory of Gases)"
            },

            ["Physics 2nd paper"] = new()
            {
                "তাপগতিবিদ্যা ও কার্নোর ইঞ্জিন (Thermodynamics & Carnot Engine)",
                "স্থির তড়িৎ ও গাউসের সূত্র (Electrostatics & Gauss's Law)",
                "চল তড়িৎ, কার্শফের সূত্র ও হুইটস্টোন ব্রিজ (Current Electricity & Kirchhoff's Laws)",
                "তড়িৎ প্রবাহের চৌম্বক ক্রিয়া ও চুম্বকত্ব (Magnetism & Biot-Savart Law)",
                "তাড়িতচৌম্বকীয় আবেশ ও পরিবর্তী প্রবাহ (Electromagnetic Induction & AC Circuits)",
                "জ্যামিতিক আলোকবিজ্ঞান (Geometrical Optics, Lenses & Optical Instruments)",
                "ভৌত আলোকবিজ্ঞান ও ব্যতিচার (Physical Optics, Interference & Diffraction)",
                "আধুনিক পদার্থবিজ্ঞানের সূচনা ও আপেক্ষিকতা (Relativity & Photoelectric Effect)",
                "পরমাণুর মডেল ও নিউক্লিয়ার পদার্থবিজ্ঞান (Atomic Models, Radioactivity & Nuclear Energy)",
                "সেমিকন্ডাক্টর, ডায়োড ও ট্রানজিস্টর (Semiconductors, Diodes & Logic Gates)",
                "জ্যোতির্বিজ্ঞান ও মহাবিশ্ব (Astronomy & Cosmology)"
            },

            ["Chemistry"] = new()
            {
                "রসায়নের ধারণা ও গবেষণার গুরুত্ব (Concepts of Chemistry)",
                "পদার্থের অবস্থা ও ব্যাপন-নিঃসরণ (States of Matter, Diffusion & Effusion)",
                "পদার্থের গঠন, আইসোটোপ ও ইলেকট্রন বিন্যাস (Atomic Structure & Electron Configuration)",
                "পর্যায় সারণি ও মৌলের পর্যায়বৃত্ত ধর্ম (Periodic Table & Properties)",
                "রাসায়নিক বন্ধন: আয়নিক ও সমযোজী (Chemical Bonds: Ionic & Covalent)",
                "মোলের ধারণা ও রাসায়নিক গণনা (Mole Concept & Stoichiometry)",
                "রাসায়নিক বিক্রিয়া ও জারণ-বিজারণ (Chemical Reactions & Redox)",
                "রসায়ন ও শক্তি (Chemistry & Energy, Electrochemical Cells)",
                "এসিড-ক্ষার সমতা ও পিএইচ (Acid-Base Balance, pH & Neutralization)",
                "খনিজ সম্পদ ও ধাতু নিষ্কাশন (Mineral Resources & Metallurgy)",
                "জীবাশ্ম জ্বালানি ও হাইড্রোকার্বন (Fossil Fuels, Hydrocarbons & Polymers)",
                "আমাদের জীবনে রসায়ন ও পরিষ্কারক সামগ্রী (Chemistry in Everyday Life)"
            },

            ["Chemistry 1st paper"] = new()
            {
                "ল্যাবরেটরির নিরাপদ ব্যবহার ও কাঁচপাত্র (Safe Laboratory Practices & Apparatus)",
                "গুণগত রসায়ন, কোয়ান্টাম সংখ্যা ও বর্ণালী (Qualitative Chemistry, Quantum Numbers & Spectra)",
                "দ্রাব্যতা ও দ্রাব্যতা গুণফল (Solubility & Solubility Product, Ksp)",
                "মৌলের পর্যায়বৃত্ত ধর্ম ও অক্সাইডসমূহ (Periodic Properties of Elements)",
                "রাসায়নিক বন্ধন ও সংকরণ/হাইব্রিডাইজেশন (Chemical Bonding & Hybridization)",
                "রাসায়নিক পরিবর্তন ও বিক্রিয়ার সাম্যাবস্থা (Chemical Equilibrium, Le Chatelier Principle)",
                "রাসায়নিক গতিবিদ্যা ও পিএইচ গণনা (Chemical Kinetics & Buffer Solutions)",
                "কর্মমুখী রসায়ন ও খাদ্য সংরক্ষণ (Applied Chemistry & Food Preservation)"
            },

            ["Chemistry 2nd paper"] = new()
            {
                "পরিবেশ রসায়ন ও বায়ুমণ্ডলের উপাদান (Environmental Chemistry & Gas Laws)",
                "জৈব রসায়ন: হাইড্রোকার্বন ও নামকরণ (Organic Chemistry: Hydrocarbons & Nomenclature)",
                "জৈব যৌগ: অ্যালকোহল, অ্যালডিহাইড, কিটোন ও এসিড (Alcohols, Carbonyls & Carboxylic Acids)",
                "অ্যারোমেটিক যৌগ ও রেজোন্যান্স (Aromatic Compounds & Resonance)",
                "জৈব যৌগের সমাণুতা (Isomerism in Organic Compounds)",
                "পরিমাণগত রসায়ন ও টাইট্রেশন (Quantitative Chemistry, Titration & Redox)",
                "তড়িৎ রসায়ন ও ফ্যারাডের সূত্র (Electrochemistry & Faraday's Laws, Nernst Equation)",
                "অর্থনৈতিক রসায়ন ও শিল্প কারখানা (Industrial Chemistry: Fertilizer, Cement, Glass & Leather)"
            },

            ["Biology"] = new()
            {
                "জীবন পাঠ ও জীববিজ্ঞানের শাখাসমূহ (Life & Branches of Biology)",
                "জীবকোষ ও টিস্যুর গঠন (Living Cells & Plant/Animal Tissues)",
                "কোষ বিভাজন: অ্যামাইটোসিস, মাইটোসিস ও মিয়োসিস (Cell Division)",
                "জীবনীশক্তি, সালোকসংশ্লেষণ ও শ্বসন (Bioenergetics, Photosynthesis & Respiration)",
                "খাদ্য, পুষ্টি এবং পরিপাকতন্ত্র (Food, Nutrition & Human Digestive System)",
                "জীবে পরিবহন, রক্ত ও হৃদপিণ্ড (Transport in Organisms, Blood & Heart)",
                "গ্যাসীয় বিনিময় ও ফুসফুস (Gaseous Exchange & Respiratory System)",
                "রেচন প্রক্রিয়া ও বৃক্ক/কিডনি (Excretory System & Kidney Structure)",
                "দৃঢ়তা প্রদান ও চলন (Skeletal System & Locomotion)",
                "সমন্বয়, মস্তিষ্ক ও হরমোন (Nervous System, Brain & Hormones)",
                "জীবের প্রজনন ও ফুলের গঠন (Reproduction in Plants & Animals)",
                "বংশগতি ও ডিএনএ/আরএনএ (Genetics, DNA, RNA & Mendel's Laws)",
                "জীবের অভিযোজন ও বাস্তুতন্ত্র (Adaptation, Food Chain & Ecosystem)",
                "জীবপ্রযুক্তি ও টিস্যু কালচার (Biotechnology & Genetic Engineering)"
            },

            ["Biology 1st paper"] = new()
            {
                "কোষ ও এর গঠন (Cell Biology, Organelles & Membrane Structure)",
                "কোষ বিভাজন (Cell Division: Mitosis & Meiosis Stages)",
                "কোষ রসায়ন: কার্বোহাইড্রেট, প্রোটিন, লিপিড ও এনজাইম (Biochemistry: Biomolecules & Enzymes)",
                "অণুজীব: ভাইরাস, ব্যাকটেরিয়া ও ম্যালেরিয়া জীবাণু (Microbiology: Viruses, Bacteria & Malaria)",
                "শৈবাল ও ছত্রাক (Algae & Fungi Structure and Reproduction)",
                "ব্রায়োফাইটা ও টেরিডোফাইটা (Bryophytes & Pteridophytes)",
                "নগ্নবীজী ও আবৃতবীজী উদ্ভিদ (Gymnosperms & Angiosperms, Malvaceae/Poaceae)",
                "টিস্যু ও টিস্যুতন্ত্র (Plant Tissue Systems & Vascular Bundles)",
                "উদ্ভিদ শারীরতত্ত্ব: প্রস্বেদন ও খনিজ লবণ শোষণ (Transpiration & Mineral Absorption)",
                "সালোকসংশ্লেষণ ও শ্বসন প্রক্রিয়া (Photosynthesis C3/C4 & Glycolysis/Krebs Cycle)",
                "উদ্ভিদ প্রজনন (Plant Reproduction, Pollination & Fertilization)",
                "জীবপ্রযুক্তি: টিস্যু কালচার ও রিকম্বিনেন্ট ডিএনএ (Biotechnology & Recombinant DNA)",
                "জীবের পরিবেশ, বিস্তার ও জীববৈচিত্র্য সংরক্ষণ (Ecology, Biomes & Conservation)"
            },

            ["Biology 2nd paper"] = new()
            {
                "প্রাণীর বিভিন্নতা ও শ্রেণিবিন্যাস (Animal Diversity & Major Phyla)",
                "প্রাণীর পরিচিতি: হাইড্রা (Animal Study: Hydra Morphology & Life Cycle)",
                "প্রাণীর পরিচিতি: ঘাসফড়িং (Animal Study: Grasshopper Anatomy & Digestion)",
                "প্রাণীর পরিচিতি: রুই মাছ (Animal Study: Rohu Fish Circulatory & Respiratory Systems)",
                "মানব শারীরতত্ত্ব: পরিপাক ও শোষণ (Human Physiology: Digestion & Absorption)",
                "মানব শারীরতত্ত্ব: রক্ত ও সংবহনতন্ত্র (Blood Groups, Circulation & Cardiac Cycle)",
                "মানব শারীরতত্ত্ব: শ্বসন ও শ্বাসক্রিয়া (Respiration Mechanism & Gas Transport)",
                "মানব শারীরতত্ত্ব: বর্জ্য ও নিষ্কাশন (Excretion, Nephron Function & Osmoregulation)",
                "মানব শারীরতত্ত্ব: চলন ও কঙ্কালতন্ত্র (Locomotion, Bones, Joints & Muscle Contraction)",
                "মানব শারীরতত্ত্ব: সমন্বয়, চোখ, কান ও স্নায়ুতন্ত্র (Nervous System, Sense Organs & Endocrine)",
                "মানব জীবনের ধারাবাহিকতা ও জননতন্ত্র (Human Reproductive System & Embryology)",
                "মানবদেহের প্রতিরক্ষা ও অনাক্রম্যতা (Immune System, Antibodies & Vaccines)",
                "জিনতত্ত্ব, মেন্ডেলের সূত্র ও লিঙ্গ নির্ধারণ (Genetics, Sex-linked Inheritance & ABO Blood)",
                "বিবর্তন ও প্রাণীর আচরণ (Evolution Theories, Animal Behavior & Innate Responses)"
            }
        };

        public static string NormalizeClassLevel(string? classLevel)
        {
            if (string.IsNullOrWhiteSpace(classLevel))
                return string.Empty;

            var clean = classLevel.Trim();
            var match = Regex.Match(clean, @"\d+");
            return match.Success ? match.Value : clean;
        }

        public static async Task<List<ClassPlan>> GetSortedClassPlansAsync(ApplicationDbContext context)
        {
            var plans = await context.ClassPlans.ToListAsync();
            if (!plans.Any())
            {
                DbInitializer.SeedClassPlans(context);
                plans = await context.ClassPlans.ToListAsync();
            }

            return plans
                .OrderBy(p => int.TryParse(NormalizeClassLevel(p.ClassLevel), out int num) ? num : 999)
                .ToList();
        }

        public static Dictionary<string, HashSet<string>> BuildValidClassSubjectsMap(IEnumerable<ClassPlan> classPlans)
        {
            var map = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

            foreach (var plan in classPlans)
            {
                var normClass = NormalizeClassLevel(plan.ClassLevel);
                if (string.IsNullOrEmpty(normClass)) continue;

                if (!map.ContainsKey(normClass))
                {
                    map[normClass] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                }

                if (plan.Subjects != null)
                {
                    foreach (var s in plan.Subjects)
                    {
                        if (!string.IsNullOrWhiteSpace(s.Name))
                        {
                            map[normClass].Add(s.Name.Trim());
                        }
                    }
                }
            }

            return map;
        }

        public static bool IsValidForClassPlan(string? classLevel, string? subject, Dictionary<string, HashSet<string>> validClassSubjectsMap)
        {
            if (string.IsNullOrWhiteSpace(classLevel) || string.IsNullOrWhiteSpace(subject))
                return false;

            var normClass = NormalizeClassLevel(classLevel);
            if (!validClassSubjectsMap.TryGetValue(normClass, out var validSubjects))
                return false;

            return validSubjects.Contains(subject.Trim());
        }

        public static List<string> GetAllDistinctSubjects(IEnumerable<ClassPlan> classPlans)
        {
            var subjects = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var plan in classPlans)
            {
                if (plan.Subjects != null)
                {
                    foreach (var s in plan.Subjects)
                    {
                        if (!string.IsNullOrWhiteSpace(s.Name))
                        {
                            subjects.Add(s.Name.Trim());
                        }
                    }
                }
            }
            return subjects.OrderBy(s => s).ToList();
        }

        public static Dictionary<string, List<string>> GetAllSubjectTopicsMap()
        {
            return new Dictionary<string, List<string>>(_subjectTopicsMap, StringComparer.OrdinalIgnoreCase);
        }

        public static List<string> GetTopicsForSubject(string? subject, string? classLevel = null)
        {
            if (string.IsNullOrWhiteSpace(subject))
                return new List<string>();

            var clean = subject.Trim();
            if (_subjectTopicsMap.TryGetValue(clean, out var topics))
            {
                return new List<string>(topics);
            }

            return new List<string>();
        }

        public static List<string> GetAllTopics()
        {
            var allTopics = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in _subjectTopicsMap)
            {
                foreach (var topic in kvp.Value)
                {
                    allTopics.Add(topic);
                }
            }
            return allTopics.OrderBy(t => t).ToList();
        }
    }
}
