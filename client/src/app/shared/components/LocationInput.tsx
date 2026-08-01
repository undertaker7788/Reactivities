import { useEffect, useMemo, useState } from "react";
import { useController, type FieldValues, type UseControllerProps } from "react-hook-form"
import type { LocationIQSuggestion } from "../../../lib/types";
import { Box, debounce, List, ListItemButton, TextField, Typography } from "@mui/material";
import axios from "axios";

type Props<T extends FieldValues> = {
    label: string;
} & UseControllerProps<T>

export default function LocationInput<T extends FieldValues>(props: Props<T>) {
    const { field, fieldState } = useController({ ...props })
    const [loading, setLoading] = useState(false);
    const [suggestions, setSuggestions] = useState<LocationIQSuggestion[]>([]);

    const [inputValue, setInputValue] = useState(field.value || '');

    useEffect(() => {
        if(field.value && typeof field.value === 'object') {
            setInputValue(field.value.venue || '');
        } else {
            setInputValue(field.value || '');
        }
    }, [field.value]);

    const locationUrl = 'https://api.locationiq.com/v1/autocomplete?key=pk.e922da39c0505bf92baade95ec965e17&limit=5&dedupe=1&';

    const fetchSuggestions = useMemo(
        // mui 提供 debounce 可以用，
        // 這裡設定 當 user 輸入後 0.5秒都沒動作時，
        // 才繼續執行後續的 function
        () => debounce(async (query: string) => {
            // query為空 或 小於3個字元
            // 就不要搜尋
            if(!query || query.length < 3) {
                setSuggestions([]);
                return;
            }

            setLoading(true);

            try {
                const res = await axios.get<LocationIQSuggestion[]>(`${locationUrl}q=${query}`);
                setSuggestions(res.data);
            } catch (error) {
                console.log(error);
            } finally {
                setLoading(false);
            }

        }, 500),
        [locationUrl]
    );

    const handleChange = async (value: string) => {
        field.onChange(value);
        await fetchSuggestions(value);
    }

    // 從 autocomplete 的選項中，
    // 選取其中一個 地點
    const handleSelect = (location: LocationIQSuggestion) => {
        // console.log(location);
        const city = location.address?.city || location.address?.town || location.address?.village || location.address?.name;
        const venue = location.display_name;
        const latitude = location.lat;
        const longitude = location.lon;

        setInputValue(venue);
        field.onChange({
            city, 
            venue, 
            latitude,
            longitude
        });
        setSuggestions([]);
    };

    return (
        <Box>
            <TextField 
                {...props}
                value={inputValue}
                onChange={e => handleChange(e.target.value)}
                fullWidth
                variant="outlined"
                error={!!fieldState.error}
                helperText={fieldState.error?.message}
            />
            { loading ?? <Typography>Loading...</Typography> }
            {
                suggestions.length > 0 && (
                    <List sx={{border: 1}}>
                        {
                            suggestions.map(suggestion => (
                                <ListItemButton
                                    divider
                                    key={suggestion.place_id}
                                    onClick={() => handleSelect(suggestion)}
                                >
                                    {suggestion.display_name}
                                </ListItemButton>
                            ))
                        }
                    </List>
                )
            }
        </Box>
    )
}