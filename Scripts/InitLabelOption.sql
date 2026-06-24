-- label_option 种子数据
-- JudgmentLabelRemark: A184-A188 (category='Judgment')
-- LanguageLabelRemark: A162-A176 (category='Language')

INSERT INTO label_option (category, sort_order, text) VALUES
('Language', 1,  'USA (Language in label should be English)'),
('Language', 2,  'EU (Language in label should be English)'),
('Language', 3,  'Canada (Language in label should be French and English)'),
('Language', 4,  'China (Language in label should be Simplified Chinese)'),
('Language', 5,  'Australia (Language in label should be English)'),
('Language', 6,  'Taiwan (Language in label should be Traditional Chinese)'),
('Language', 7,  'Mexico (Language in label should be Spanish)'),
('Language', 8,  'Japan (Language in label should be Japanese)'),
('Language', 9,  'New Zealand (Language in label should be English)'),
('Language', 10, 'England (Language in label should be English)'),
('Language', 11, 'Spain (Language in label should be Spanish)'),
('Language', 12, 'France (Language in label should be French)'),
('Language', 13, 'Iceland (Language in label should be Icelandic)'),
('Language', 14, 'Korea (Language in label should be Korean)'),
('Language', 15, 'Sweden (Language in label should be Swedish)'),

('Judgment', 1, 'Conclusion: The information listed on the fibre content label is appropriate'),
('Judgment', 2, 'Conclusion: The information listed on the fibre content label is inappropriate, our recommendation as below:'),
('Judgment', 3, 'The fiber label provided by client is appropriate.'),
('Judgment', 4, 'The fiber label provided by client is inappropriate because of '),
('Judgment', 5, 'The fiber label provided by client is appropriate, but the following fiber content label would be more appropriate for the submitted product:');
