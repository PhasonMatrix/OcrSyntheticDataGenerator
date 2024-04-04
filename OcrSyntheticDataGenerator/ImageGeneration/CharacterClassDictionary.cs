using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OcrSyntheticDataGenerator.ImageGeneration
{
    public class CharacterClassDictionary
    {

        public static Dictionary<char, string> CharacterClasses = new Dictionary<char, string>()
        {
            {'a', "lower_case_a"},
            {'b', "lower_case_b"},
            {'c', "lower_case_c"},
            {'d', "lower_case_d"},
            {'e', "lower_case_e"},
            {'f', "lower_case_f"},
            {'g', "lower_case_g"},
            {'h', "lower_case_h"},
            {'i', "lower_case_i"},
            {'j', "lower_case_j"},
            {'k', "lower_case_k"},
            {'l', "lower_case_l"},
            {'m', "lower_case_m"},
            {'n', "lower_case_n"},
            {'o', "lower_case_o"},
            {'p', "lower_case_p"},
            {'q', "lower_case_q"},
            {'r', "lower_case_r"},
            {'s', "lower_case_s"},
            {'t', "lower_case_t"},
            {'u', "lower_case_u"},
            {'v', "lower_case_v"},
            {'w', "lower_case_w"},
            {'x', "lower_case_x"},
            {'y', "lower_case_y"},
            {'z', "lower_case_z"},

            {'A', "upper_case_A"},
            {'B', "upper_case_B"},
            {'C', "upper_case_C"},
            {'D', "upper_case_D"},
            {'E', "lower_case_E"},
            {'F', "lower_case_F"},
            {'G', "lower_case_G"},
            {'H', "lower_case_H"},
            {'I', "lower_case_I"},
            {'J', "lower_case_J"},
            {'K', "lower_case_K"},
            {'L', "lower_case_L"},
            {'M', "lower_case_M"},
            {'N', "lower_case_N"},
            {'O', "lower_case_O"},
            {'P', "lower_case_P"},
            {'Q', "lower_case_Q"},
            {'R', "lower_case_R"},
            {'S', "lower_case_S"},
            {'T', "lower_case_T"},
            {'U', "lower_case_U"},
            {'V', "lower_case_V"},
            {'W', "lower_case_W"},
            {'X', "lower_case_X"},
            {'Y', "lower_case_Y"},
            {'Z', "lower_case_Z"},

            {'0', "numeral_0"},
            {'1', "numeral_1"},
            {'2', "numeral_2"},
            {'3', "numeral_3"},
            {'4', "numeral_4"},
            {'5', "numeral_5"},
            {'6', "numeral_6"},
            {'7', "numeral_7"},
            {'8', "numeral_8"},
            {'9', "numeral_9"},

            {'.', "symbol_dot"},
            {',', "symbol_comma"},
            {'?', "symbol_question_mark"},
            {'!', "symbol_exclamation_mark"},
            {':', "symbol_colon"},
            {';', "symbol_semicolon"},
            {'@', "symbol_at"},
            {'#', "symbol_hash"},
            {'$', "symbol_dollar"},
            {'%', "symbol_percent"},
            {'^', "symbol_hat"},
            {'&', "symbol_ampersand"},
            {'*', "symbol_asterisk"},
            {'|', "symbol_pipe"},

            {'-', "symbol_minus"},
            {'_', "symbol_underscore"},
            {'+', "symbol_plus"},
            {'=', "symbol_equals"},

            {'(', "symbol_perenthesis_left"},
            {')', "symbol_perenthesis_right"},
            {'[', "symbol_square_bracket_left"},
            {']', "symbol_square_bracket_right"},
            {'{', "symbol_curly_brace_left"},
            {'}', "symbol_curly_brack_right"},
            {'<', "symbol_angle_bracket_left"},
            {'>', "symbol_angle_bracket_right"},

            {'"', "symbol_double_quote_nutral"},
            {'“', "symbol_double_quote_left"},
            {'”', "symbol_double_quote_right"},
            {'\'', "symbol_single_quote_nutral"},
            {'‘', "symbol_single_quote_left"},
            {'’', "symbol_single_quote_right"},
            {'«', "symbol_guillemet_left"},
            {'»', "symbol_guillemet_right"},
            {'©', "symbol_copyright"},
            {'®', "symbol_registration_mark"},
            {'§', "symbol_section_sign"}
        };

    }
}
