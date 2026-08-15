import arEG from 'antd/locale/ar_EG'
import bgBG from 'antd/locale/bg_BG'
import csCZ from 'antd/locale/cs_CZ'
import deDE from 'antd/locale/de_DE'
import elGR from 'antd/locale/el_GR'
import enUS from 'antd/locale/en_US'
import esES from 'antd/locale/es_ES'
import frFR from 'antd/locale/fr_FR'
import huHU from 'antd/locale/hu_HU'
import itIT from 'antd/locale/it_IT'
import jaJP from 'antd/locale/ja_JP'
import lvLV from 'antd/locale/lv_LV'
import nlNL from 'antd/locale/nl_NL'
import plPL from 'antd/locale/pl_PL'
import ptBR from 'antd/locale/pt_BR'
import ptPT from 'antd/locale/pt_PT'
import roRO from 'antd/locale/ro_RO'
import ruRU from 'antd/locale/ru_RU'
import skSK from 'antd/locale/sk_SK'
import trTR from 'antd/locale/tr_TR'
import ukUA from 'antd/locale/uk_UA'
import uzUZ from 'antd/locale/uz_UZ'
import viVN from 'antd/locale/vi_VN'
import zhCN from 'antd/locale/zh_CN'
import zhTW from 'antd/locale/zh_TW'

type AntDesignLocale = typeof enUS

const ANT_DESIGN_LOCALES: Record<string, AntDesignLocale> = {
  ar: arEG,
  bg: bgBG,
  cs: csCZ,
  de: deDE,
  el: elGR,
  en: enUS,
  es: esES,
  fr: frFR,
  hu: huHU,
  it: itIT,
  ja: jaJP,
  lv: lvLV,
  'nl-NL': nlNL,
  pl: plPL,
  pt: ptPT,
  'pt-BR': ptBR,
  ro: roRO,
  ru: ruRU,
  sk: skSK,
  tr: trTR,
  uk: ukUA,
  vi: viVN,
  'uz-Latn-UZ': uzUZ,
  'zh-CN': zhCN,
  'zh-Hant': zhTW
}

export function getAntDesignLocale(language: string): AntDesignLocale {
  return ANT_DESIGN_LOCALES[language] ?? enUS
}
